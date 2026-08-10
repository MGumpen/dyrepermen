using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dyrepermen.Infrastructure.Services;

/// <summary>
/// Merk at fornyelse av innloggingskapselen IKKE skjer her.
/// <c>SignInManager</c> bor i ASP.NET Core-rammeverket, og a dra hele
/// webrammeverket inn i datalaget for ett kall er feil vei. Controlleren
/// kaller <c>RefreshSignInAsync</c> etter at denne har returnert.
/// </summary>
public sealed class HusstandService : IHusstandService
{
    private readonly DyrepermenDbContext _db;
    private readonly IHusstandContext _husstand;
    private readonly ILogger<HusstandService> _log;

    public HusstandService(
        DyrepermenDbContext db,
        IHusstandContext husstand,
        ILogger<HusstandService> log)
    {
        _db = db;
        _husstand = husstand;
        _log = log;
    }

    public async Task<int> OpprettHusstand(
        string navn, int brukerId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var husstand = new Husstand { Navn = navn };
        _db.Husstand.Add(husstand);
        await _db.SaveChangesAsync(ct);

        // Ma opprettes i samme transaksjon. Mangler raden, faller OpprettDyr
        // tilbake pa hardkodede standardverdier og innstillingssiden krasjer
        // pa en null-referanse. Plan kapittel 12.2.
        _db.HusstandInnstilling.Add(new HusstandInnstilling
        {
            HusstandId = husstand.Id
        });

        var bruker = await _db.Users.SingleAsync(u => u.Id == brukerId, ct);
        bruker.HusstandId = husstand.Id;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _log.LogInformation(
            "Husstand {HusstandId} opprettet av {BrukerId}",
            husstand.Id, brukerId);

        return husstand.Id;
    }

    public async Task<Husstandsoversikt?> HentOversikt(
        int brukerId, CancellationToken ct)
    {
        var id = _husstand.HusstandId;
        if (id == 0)
        {
            return null;
        }

        var husstand = await _db.Husstand
            .Where(h => h.Id == id)
            .Select(h => new { h.Id, h.Navn })
            .SingleOrDefaultAsync(ct);

        if (husstand is null)
        {
            return null;
        }

        var medlemmer = await _db.Users
            .Where(u => u.HusstandId == id)
            .OrderBy(u => u.Visningsnavn)
            .Select(u => new Husstandsmedlem(
                u.Id, u.Visningsnavn, u.Email, u.Id == brukerId))
            .ToListAsync(ct);

        var ventende = await _db.HusstandInvitasjon
            .Where(i => i.InnlostTid == null)
            .OrderBy(i => i.OpprettetDato)
            .Select(i => new VentendeInvitasjon(i.Id, i.Epost, i.OpprettetDato))
            .ToListAsync(ct);

        var innstilling = await _db.HusstandInnstilling
            .SingleOrDefaultAsync(i => i.HusstandId == id, ct);

        return new Husstandsoversikt(
            husstand.Id,
            husstand.Navn,
            medlemmer,
            ventende,
            innstilling?.ForingsloggStandard ?? false,
            innstilling?.ForplanStandard ?? true,
            innstilling?.VarslerAktiv ?? true,
            await _db.Dyr.CountAsync(ct));
    }

    public async Task<LeggTilResultat> LeggTilMedlem(
        string epost, int utfortAvBrukerId, CancellationToken ct)
    {
        var husstandId = _husstand.HusstandId;
        var normalisert = epost.Trim().ToLowerInvariant();

        var eksisterende = await _db.Users.SingleOrDefaultAsync(
            u => u.NormalizedEmail == normalisert.ToUpperInvariant(), ct);

        if (eksisterende is not null)
        {
            if (eksisterende.HusstandId == husstandId)
            {
                return LeggTilResultat.AlleredeMedlem;
            }

            // ---------------------------------------------------------------
            // Den viktigste linjen i metoden.
            //
            // Uten den kan hvem som helst taste inn e-postadressen til en
            // fremmed bruker og flytte dem ut av deres egen husstand, siden
            // husstand_id er en enkeltverdi. Den forrige husstanden mister et
            // medlem uten varsel - og er det siste medlem, blir alle dataene
            // utilgjengelige.
            //
            // Dette er ikke et teoretisk scenario. Det er den forventede
            // oppforselen hvis sjekken mangler. Se plan kapittel 12.3.
            // ---------------------------------------------------------------
            if (eksisterende.HusstandId is not null)
            {
                _log.LogWarning(
                    "Forsok pa a legge til bruker {BrukerId} som allerede "
                    + "tilhorer en annen husstand", eksisterende.Id);

                return LeggTilResultat.TilhorerAnnenHusstand;
            }

            eksisterende.HusstandId = husstandId;
            await _db.SaveChangesAsync(ct);

            _log.LogInformation(
                "Bruker {BrukerId} lagt til i husstand {HusstandId}",
                eksisterende.Id, husstandId);

            return LeggTilResultat.LagtTil;
        }

        // Adressen finnes ikke enna. Invitasjonen loses inn automatisk nar
        // noen registrerer seg med noyaktig denne adressen.
        var alt = await _db.HusstandInvitasjon
            .AnyAsync(i => i.Epost == normalisert && i.InnlostTid == null, ct);

        if (!alt)
        {
            _db.HusstandInvitasjon.Add(new HusstandInvitasjon
            {
                HusstandId = husstandId,
                Epost = normalisert,
                OpprettetAvBrukerId = utfortAvBrukerId
            });

            await _db.SaveChangesAsync(ct);
        }

        return LeggTilResultat.VenterPaRegistrering;
    }

    public async Task<bool> AngreInvitasjon(int invitasjonId, CancellationToken ct)
    {
        var rad = await _db.HusstandInvitasjon.SingleOrDefaultAsync(
            i => i.Id == invitasjonId && i.InnlostTid == null, ct);

        if (rad is null)
        {
            return false;
        }

        _db.HusstandInvitasjon.Remove(rad);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> FjernMedlem(int brukerId, CancellationToken ct)
    {
        var husstandId = _husstand.HusstandId;

        var bruker = await _db.Users.SingleOrDefaultAsync(
            u => u.Id == brukerId && u.HusstandId == husstandId, ct);

        if (bruker is null)
        {
            return false;
        }

        // Applikasjonen skal ikke tillate en husstand uten medlemmer - da
        // blir dataene utilgjengelige for alltid uten a bli slettet.
        var antall = await _db.Users.CountAsync(u => u.HusstandId == husstandId, ct);
        if (antall <= 1)
        {
            return false;
        }

        bruker.HusstandId = null;
        await _db.SaveChangesAsync(ct);

        _log.LogInformation(
            "Bruker {BrukerId} fjernet fra husstand {HusstandId}",
            brukerId, husstandId);

        return true;
    }

    public async Task<bool> LagreInnstillinger(
        string husstandsnavn,
        bool foringsloggStandard,
        bool forplanStandard,
        bool varslerAktiv,
        CancellationToken ct)
    {
        var id = _husstand.HusstandId;

        var husstand = await _db.Husstand.SingleOrDefaultAsync(h => h.Id == id, ct);
        if (husstand is null)
        {
            return false;
        }

        husstand.Navn = husstandsnavn.Trim();

        var innstilling = await _db.HusstandInnstilling
            .SingleOrDefaultAsync(i => i.HusstandId == id, ct);

        if (innstilling is null)
        {
            innstilling = new HusstandInnstilling { HusstandId = id };
            _db.HusstandInnstilling.Add(innstilling);
        }

        // Standardverdier for NYE dyr. De overstyrer aldri en bryter som
        // allerede star pa et dyr. Se plan kapittel 8.2.
        innstilling.ForingsloggStandard = foringsloggStandard;
        innstilling.ForplanStandard = forplanStandard;
        innstilling.VarslerAktiv = varslerAktiv;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> LosInnInvitasjon(
        int brukerId, string epost, CancellationToken ct)
    {
        var normalisert = epost.Trim().ToLowerInvariant();

        // IgnoreQueryFilters er noedvendig her: brukeren har enna ingen
        // husstand, sa HusstandId er 0 og filteret ville skjult alt.
        var invitasjon = await _db.HusstandInvitasjon
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                i => i.Epost == normalisert && i.InnlostTid == null, ct);

        if (invitasjon is null)
        {
            return false;
        }

        var bruker = await _db.Users.SingleAsync(u => u.Id == brukerId, ct);
        bruker.HusstandId = invitasjon.HusstandId;

        invitasjon.InnlostAvBrukerId = brukerId;
        invitasjon.InnlostTid = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        _log.LogInformation(
            "Bruker {BrukerId} loste inn invitasjon til husstand {HusstandId}",
            brukerId, invitasjon.HusstandId);

        return true;
    }
}
