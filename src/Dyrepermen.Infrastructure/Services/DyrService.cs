using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Dyrepermen.Infrastructure.Services;

public sealed class DyrService : IDyrService
{
    private readonly DyrepermenDbContext _db;
    private readonly IHusstandContext _husstand;
    private readonly ILogger<DyrService> _log;

    public DyrService(
        DyrepermenDbContext db,
        IHusstandContext husstand,
        ILogger<DyrService> log)
    {
        _db = db;
        _husstand = husstand;
        _log = log;
    }

    public async Task<IReadOnlyList<DyrListeElement>> HentAlle(CancellationToken ct)
        // Projiser i sporringen. Aldri Include etterfulgt av .Last() i C#.
        => await _db.Dyr
            .OrderBy(d => d.Navn)
            .Select(d => new DyrListeElement(
                d.Id, d.Navn, d.Art, d.Rase, d.Fodselsdato))
            .ToListAsync(ct);

    public async Task<DyrDetaljer?> HentDetaljer(int dyrId, CancellationToken ct)
        // Query-filteret er autorisasjonen: et dyr i en annen husstand gir
        // null, som controlleren gjor om til 404.
        => await _db.Dyr
            .Where(d => d.Id == dyrId)
            .Select(d => new DyrDetaljer(
                d.Id, d.Navn, d.Art, d.Kjonn, d.Rase, d.Fodselsdato,
                d.ChipNr, d.RegNrNkk, d.Kastrert,
                d.ForingsloggAktiv, d.ForplanAktiv))
            .SingleOrDefaultAsync(ct);

    public async Task<DyrSammendrag?> HentSammendrag(
        int dyrId, CancellationToken ct)
    {
        var idag = DateOnly.FromDateTime(DateTime.UtcNow);

        // Korrelerte undersporringer i ett Select. Npgsql oversetter dem til
        // LEFT JOIN LATERAL, sa hele sammendraget kommer i en rundtur.
        var rad = await _db.Dyr
            .Where(d => d.Id == dyrId)
            .Select(d => new
            {
                AntallVekter = d.Vekter.Count(),
                SisteVekt = d.Vekter
                    .OrderByDescending(v => v.Dato)
                    .ThenByDescending(v => v.Id)
                    .Select(v => new { v.VektGram, v.Dato })
                    .FirstOrDefault(),

                AntallBehandlinger = d.Behandlinger.Count(),
                Neste = d.Behandlinger
                    .Where(b => b.NesteDato != null)
                    .OrderBy(b => b.NesteDato)
                    .Select(b => new { b.Type, b.Preparat, Dato = b.NesteDato!.Value })
                    .FirstOrDefault(),

                AntallMedisiner = d.Medisiner.Count(),
                Aktive = d.Medisiner
                    .Where(m => m.SluttDato == null || m.SluttDato >= idag)
                    .OrderBy(m => m.Navn)
                    .Select(m => m.Navn + " – " + m.Dose)
                    .ToList(),

                Forplan = d.Forplaner
                    .Where(f => f.Aktiv)
                    .Select(f => new
                    {
                        f.Metode,
                        f.ProsentTidels,
                        f.GramPerDag,
                        f.AntallMaltider
                    })
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync(ct);

        if (rad is null)
        {
            return null;
        }

        var notater = await _db.Informasjon.CountAsync(i => i.DyrId == dyrId, ct);

        return new DyrSammendrag(
            rad.AntallVekter,
            rad.SisteVekt?.VektGram,
            rad.SisteVekt?.Dato,
            rad.AntallBehandlinger,
            rad.Neste is null ? null : Behandlingstekst(rad.Neste.Type, rad.Neste.Preparat),
            rad.Neste?.Dato,
            rad.AntallMedisiner,
            rad.Aktive,
            Forplantekst(
                rad.Forplan?.Metode, rad.Forplan?.ProsentTidels,
                rad.Forplan?.GramPerDag, rad.Forplan?.AntallMaltider,
                rad.SisteVekt?.VektGram),
            notater);
    }

    private static string Behandlingstekst(BehandlingType type, string? preparat)
    {
        var navn = type switch
        {
            BehandlingType.Vaksine => "Vaksine",
            BehandlingType.Ormekur => "Ormekur",
            BehandlingType.Flatt => "Flåttmiddel",
            BehandlingType.Kloklipp => "Kloklipp",
            BehandlingType.Tannrens => "Tannrens",
            _ => "Behandling"
        };

        return preparat is null ? navn : $"{navn} – {preparat}";
    }

    /// <summary>
    /// Samme regel som ForplanService. Uten vektgrunnlag sier den fra
    /// framfor a vise et tall uten dekning.
    /// </summary>
    private static string? Forplantekst(
        Formetode? metode, int? prosentTidels, int? gramPerDag,
        int? antallMaltider, int? sisteVektGram)
    {
        if (metode is null)
        {
            return null;
        }

        var maltider = antallMaltider ?? 2;

        if (metode == Formetode.Gram)
        {
            return $"{gramPerDag} g/dag på {maltider} måltider";
        }

        if (sisteVektGram is null)
        {
            return "Mangler vektregistrering";
        }

        var gram = (int)Math.Round(
            sisteVektGram.Value * prosentTidels!.Value / 1000.0,
            MidpointRounding.AwayFromZero);

        return $"{gram} g/dag på {maltider} måltider";
    }

    public async Task<DyrResultat> Opprett(NyttDyr input, CancellationToken ct)
    {
        // Standardverdiene kopieres inn ved opprettelse. De overstyrer aldri
        // en bryter som allerede star pa et dyr. Se plan kapittel 8.2.
        var std = await _db.HusstandInnstilling
            .SingleOrDefaultAsync(i => i.HusstandId == _husstand.HusstandId, ct);

        var dyr = new Dyr
        {
            HusstandId = _husstand.HusstandId,
            Navn = input.Navn.Trim(),
            Art = input.Art,
            Kjonn = input.Kjonn,
            Rase = input.Rase.TomTilNull(),
            Fodselsdato = input.Fodselsdato,
            // TomTilNull er ikke kosmetikk: tom streng er en verdi i den
            // partielle unike indeksen, mens NULL ikke deltar. Uten den
            // kolliderer dyr nummer to uten chipnummer.
            ChipNr = input.ChipNr.TomTilNull(),
            // NKK-registeret gjelder hund. En katt skal aldri ha regnummer,
            // uansett hva som ble postet.
            RegNrNkk = input.Art == Art.Hund
                ? input.RegNrNkk.TomTilNull()?.ToUpperInvariant()
                : null,
            Kastrert = input.Kastrert,
            ForingsloggAktiv = std?.ForingsloggStandard ?? false,
            ForplanAktiv = std?.ForplanStandard ?? true
        };

        _db.Dyr.Add(dyr);

        var feil = await LagreOgOversett(ct);
        if (feil is not DyrFeil.Ingen)
        {
            _db.Entry(dyr).State = EntityState.Detached;
            return DyrResultat.Avvist(feil.Value);
        }

        _log.LogInformation(
            "Dyr {DyrId} opprettet i husstand {HusstandId}",
            dyr.Id, dyr.HusstandId);

        return DyrResultat.Lagret(dyr.Id);
    }

    public async Task<DyrResultat> Oppdater(RedigerDyr input, CancellationToken ct)
    {
        var dyr = await _db.Dyr.SingleOrDefaultAsync(d => d.Id == input.Id, ct);
        if (dyr is null)
        {
            return DyrResultat.Avvist(DyrFeil.FinnesIkke);
        }

        dyr.Navn = input.Navn.Trim();
        dyr.Art = input.Art;
        dyr.Kjonn = input.Kjonn;
        dyr.Rase = input.Rase.TomTilNull();
        dyr.Fodselsdato = input.Fodselsdato;
        dyr.ChipNr = input.ChipNr.TomTilNull();
        dyr.RegNrNkk = input.Art == Art.Hund
            ? input.RegNrNkk.TomTilNull()?.ToUpperInvariant()
            : null;
        dyr.Kastrert = input.Kastrert;
        dyr.ForingsloggAktiv = input.ForingsloggAktiv;
        dyr.ForplanAktiv = input.ForplanAktiv;

        var feil = await LagreOgOversett(ct);
        return feil is DyrFeil.Ingen
            ? DyrResultat.Lagret(dyr.Id)
            : DyrResultat.Avvist(feil.Value);
    }

    public async Task<bool> Deaktiver(int dyrId, CancellationToken ct)
    {
        var dyr = await _db.Dyr.SingleOrDefaultAsync(d => d.Id == dyrId, ct);
        if (dyr is null)
        {
            return false;
        }

        // Dyr slettes ikke. Historikk om et dyr som er gatt bort skal bevares,
        // og chipnummeret forblir opptatt i indeksen. Se plan kapittel 5.2.
        dyr.Aktiv = false;
        await _db.SaveChangesAsync(ct);

        _log.LogInformation("Dyr {DyrId} deaktivert", dyrId);
        return true;
    }

    /// <summary>
    /// Returnerer DyrFeil.Ingen ved suksess.
    ///
    /// Kollisjonen kan komme fra en rad du ikke ser: query-filteret skjuler
    /// deaktiverte dyr, men den unike indeksen ser dem. Meldingen controlleren
    /// lager ma derfor vaere noytral og ikke avslore hvilken husstand dyret
    /// tilhorer - unikheten er global. Se plan kapittel 5.3.
    /// </summary>
    private async Task<DyrFeil?> LagreOgOversett(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
            return DyrFeil.Ingen;
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: "23505" } pg)
        {
            _log.LogWarning(
                "Unikhetsbrudd pa {Constraint}", pg.ConstraintName);

            return pg.ConstraintName switch
            {
                "ux_dyr_chip" => DyrFeil.ChipFinnes,
                "ux_dyr_regnr" => DyrFeil.RegnrFinnes,
                _ => DyrFeil.ChipFinnes
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            _log.LogWarning("Samtidighetskonflikt ved lagring av dyr");
            return DyrFeil.Samtidighet;
        }
    }
}
