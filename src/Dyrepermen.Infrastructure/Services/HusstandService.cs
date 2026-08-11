using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dyrepermen.Infrastructure.Services;

/// <summary>
/// Merk at fornyelse av innloggingskapselen IKKE skjer her.
/// <c>SignInManager</c> bor i ASP.NET Core-rammeverket, og a dra hele
/// webrammeverket inn i datalaget for ett kall er feil vei.
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

        var husstand = new Husstand { Navn = navn.Trim() };
        _db.Husstand.Add(husstand);
        await _db.SaveChangesAsync(ct);

        // Ma opprettes i samme transaksjon. Mangler raden, faller OpprettDyr
        // tilbake pa hardkodede standardverdier og innstillingssiden krasjer
        // pa en null-referanse. Plan kapittel 12.2.
        _db.HusstandInnstilling.Add(new HusstandInnstilling
        {
            HusstandId = husstand.Id
        });

        // Den som oppretter husstanden bor i den.
        _db.Husstandsmedlemskap.Add(new Husstandsmedlemskap
        {
            HusstandId = husstand.Id,
            BrukerId = brukerId,
            Rolle = Husstandsrolle.Beboer
        });

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

        var medlemmer = await _db.Husstandsmedlemskap
            .Where(m => m.HusstandId == id)
            .OrderBy(m => m.Rolle)
            .ThenBy(m => m.Bruker.Visningsnavn)
            .Select(m => new Husstandsmedlem(
                m.BrukerId, m.Bruker.Visningsnavn, m.Bruker.Email,
                m.BrukerId == brukerId, m.Rolle))
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
            // Mangler raden, gjelder standardverdien fra Domain.
            innstilling?.GodbitloggAktiv ?? true,
            await _db.Dyr.CountAsync(ct));
    }

    public async Task<LeggTilResultat> LeggTilMedlem(
        string epost, Husstandsrolle rolle, int utfortAvBrukerId,
        CancellationToken ct)
    {
        var husstandId = _husstand.HusstandId;
        var normalisert = epost.Trim().ToLowerInvariant();

        var eksisterende = await _db.Users.SingleOrDefaultAsync(
            u => u.NormalizedEmail == normalisert.ToUpperInvariant(), ct);

        if (eksisterende is not null)
        {
            var alt = await _db.Husstandsmedlemskap.AnyAsync(
                m => m.HusstandId == husstandId && m.BrukerId == eksisterende.Id, ct);

            if (alt)
            {
                return LeggTilResultat.AlleredeMedlem;
            }

            // -------------------------------------------------------------
            // Her sto tidligere prosjektets viktigste sikkerhetssjekk: at
            // adressen ikke allerede tilhorte en annen husstand.
            //
            // Den var nodvendig fordi husstand_id var EN kolonne - a legge
            // noen til i din husstand flyttet dem ut av deres egen, og var
            // de siste medlem, ble dataene deres utilgjengelige.
            //
            // Med medlemskap i en egen tabell finnes ikke den faren lenger.
            // A legge noen til her tar ingenting fra dem andre steder.
            // Sjekken er derfor fjernet, ikke glemt. Se ADR 0009.
            // -------------------------------------------------------------
            _db.Husstandsmedlemskap.Add(new Husstandsmedlemskap
            {
                HusstandId = husstandId,
                BrukerId = eksisterende.Id,
                Rolle = rolle
            });

            await _db.SaveChangesAsync(ct);

            _log.LogInformation(
                "Bruker {BrukerId} lagt til i husstand {HusstandId} som {Rolle}",
                eksisterende.Id, husstandId, rolle);

            return LeggTilResultat.LagtTil;
        }

        // Adressen finnes ikke enna. Invitasjonen loses inn automatisk nar
        // noen registrerer seg med noyaktig denne adressen.
        var venter = await _db.HusstandInvitasjon
            .AnyAsync(i => i.Epost == normalisert && i.InnlostTid == null, ct);

        if (!venter)
        {
            _db.HusstandInvitasjon.Add(new HusstandInvitasjon
            {
                HusstandId = husstandId,
                Epost = normalisert,
                Rolle = rolle,
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

        var medlemskap = await _db.Husstandsmedlemskap.SingleOrDefaultAsync(
            m => m.HusstandId == husstandId && m.BrukerId == brukerId, ct);

        if (medlemskap is null)
        {
            return false;
        }

        // En husstand ma ha minst en beboer. Uten det blir innstillingene
        // og medlemslisten last for alle - gjester kan ikke endre dem.
        if (medlemskap.Rolle == Husstandsrolle.Beboer
            && await AntallBeboere(husstandId, ct) <= 1)
        {
            return false;
        }

        _db.Husstandsmedlemskap.Remove(medlemskap);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation(
            "Bruker {BrukerId} fjernet fra husstand {HusstandId}",
            brukerId, husstandId);

        return true;
    }

    public async Task<bool> EndreRolle(
        int brukerId, Husstandsrolle rolle, CancellationToken ct)
    {
        var husstandId = _husstand.HusstandId;

        var medlemskap = await _db.Husstandsmedlemskap.SingleOrDefaultAsync(
            m => m.HusstandId == husstandId && m.BrukerId == brukerId, ct);

        if (medlemskap is null || medlemskap.Rolle == rolle)
        {
            return false;
        }

        // Samme regel som ved fjerning: den siste beboeren kan ikke
        // degraderes til gjest.
        if (medlemskap.Rolle == Husstandsrolle.Beboer
            && await AntallBeboere(husstandId, ct) <= 1)
        {
            return false;
        }

        medlemskap.Rolle = rolle;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private Task<int> AntallBeboere(int husstandId, CancellationToken ct)
        => _db.Husstandsmedlemskap.CountAsync(
            m => m.HusstandId == husstandId && m.Rolle == Husstandsrolle.Beboer, ct);

    public async Task<bool> LagreInnstillinger(
        string husstandsnavn,
        bool foringsloggStandard,
        bool forplanStandard,
        bool varslerAktiv,
        bool godbitloggAktiv,
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
        innstilling.GodbitloggAktiv = godbitloggAktiv;

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

        _db.Husstandsmedlemskap.Add(new Husstandsmedlemskap
        {
            HusstandId = invitasjon.HusstandId,
            BrukerId = brukerId,
            Rolle = invitasjon.Rolle
        });

        invitasjon.InnlostAvBrukerId = brukerId;
        invitasjon.InnlostTid = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        _log.LogInformation(
            "Bruker {BrukerId} loste inn invitasjon til husstand {HusstandId}",
            brukerId, invitasjon.HusstandId);

        return true;
    }
}
