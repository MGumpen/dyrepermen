using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Infrastructure.Services;

public sealed class BehandlingService : IBehandlingService
{
    private readonly DyrepermenDbContext _db;

    public BehandlingService(DyrepermenDbContext db) => _db = db;

    /// <summary>
    /// Hvor mange tidligere behandlinger forslagene bygges av. Nok til a
    /// dekke flere ar med ormekur og vaksiner for et helt kobbel, uten at en
    /// husstand med lang historikk henter alt hver gang skjemaet apnes.
    /// </summary>
    private const int ForslagGrunnlag = 200;

    private const int AntallForslag = 12;

    public async Task<IReadOnlyList<BehandlingRad>> HentFor(
        int dyrId, CancellationToken ct)
    {
        var rader = await _db.Behandling
            .Where(b => b.DyrId == dyrId)
            .OrderByDescending(b => b.Dato)
            .ThenByDescending(b => b.Id)
            .Select(b => new BehandlingRad(
                b.Id, b.Type, b.Preparat, b.Dato, b.NesteDato, b.Notat,
                b.AntallDager, b.GangerPerDag, false))
            .ToListAsync(ct);

        // Egen rundtur for hvilke som er apne. Regelen er et uttrykk over
        // entiteten og lar seg ikke bruke inne i projeksjonen over - og den
        // skal ikke skrives en gang til i C#, for da spriker de.
        var apne = (await _db.Behandling
            .Where(b => b.DyrId == dyrId)
            .Where(Behandlingsfilter.ApenPaminnelse)
            .Select(b => b.Id)
            .ToListAsync(ct))
            .ToHashSet();

        return rader
            .Select(r => apne.Contains(r.Id) ? r with { ErApen = true } : r)
            .ToList();
    }

    public async Task<Gittgrunnlag?> HentGittgrunnlag(
        int dyrId, int behandlingId, CancellationToken ct)
    {
        var forrige = await _db.Behandling
            .Where(b => b.Id == behandlingId && b.DyrId == dyrId)
            .Select(b => new
            {
                b.Type,
                b.Preparat,
                b.Dato,
                b.NesteDato,
                DyreNavn = b.Dyr.Navn
            })
            .SingleOrDefaultAsync(ct);

        if (forrige is null)
        {
            return null;
        }

        var idag = Tidssone.Idag(DateTimeOffset.UtcNow);

        return new Gittgrunnlag(
            dyrId,
            behandlingId,
            forrige.DyreNavn,
            Behandlingsformat.MedPreparat(forrige.Type, forrige.Preparat),
            forrige.Dato,
            forrige.NesteDato,
            Behandlingsintervall.KanGisIgjen(forrige.Dato, idag)
                && await ErApen(behandlingId, ct));
    }

    public async Task<GittResultat> Gitt(
        int dyrId, int behandlingId, DateOnly? nesteDato, int? brukerId,
        CancellationToken ct)
    {
        // Query-filteret er autorisasjonen, DyrId hindrer at en id fra et
        // annet dyr i egen husstand treffer. Samme monster som Oppdater.
        var forrige = await _db.Behandling
            .Where(b => b.Id == behandlingId && b.DyrId == dyrId)
            .Select(b => new
            {
                b.Type, b.Preparat, b.Dato, b.AntallDager, b.GangerPerDag
            })
            .SingleOrDefaultAsync(ct);

        if (forrige is null)
        {
            return GittResultat.FinnesIkke();
        }

        var idag = Tidssone.Idag(DateTimeOffset.UtcNow);

        // Gitt i dag allerede: en ny rad ville fatt samme dato, og sett ut
        // som ingenting skjedde. Sjekkes her og ikke bare ved knappen - en
        // fane som har statt apen siden i gar, viser fortsatt knappen.
        if (!Behandlingsintervall.KanGisIgjen(forrige.Dato, idag))
        {
            return GittResultat.GittIdag();
        }

        // En rad som allerede er fulgt opp, skal ikke gi en behandling til.
        // Uten sjekken ville et dobbelttrykk, eller to i husstanden som
        // krysser av samtidig fra hver sin telefon, registrert ormekuren to
        // ganger.
        if (!await ErApen(behandlingId, ct))
        {
            return GittResultat.AlleredeFulgtOpp();
        }

        var ny = new Behandling
        {
            DyrId = dyrId,
            Type = forrige.Type,
            // Preparatet kopieres som det sto. Det er det som gjor at den
            // nye raden gjenkjennes som oppfolgingen av den gamle.
            Preparat = forrige.Preparat,
            Dato = idag,
            NesteDato = nesteDato,
            // Neste ormekur er en tredagerskur den ogsa.
            AntallDager = forrige.AntallDager,
            GangerPerDag = forrige.GangerPerDag
        };

        // En kur har doser, og "gitt i dag" betyr at dagens forste er gitt.
        // Uten den ville raden statt pa oversikten med "0/1 i dag" rett
        // etter at brukeren sa den var gitt. Ny behandling, sa ingen andre
        // kan ha logget doser pa den - ingen las trengs.
        if (Behandlingskur.ErKur(forrige.AntallDager, forrige.GangerPerDag))
        {
            ny.Doser.Add(new BehandlingDose
            {
                GittTid = DateTimeOffset.UtcNow,
                GittAvBrukerId = brukerId
            });
        }

        _db.Behandling.Add(ny);
        await _db.SaveChangesAsync(ct);

        return GittResultat.Lagret(
            Behandlingsformat.MedPreparat(forrige.Type, forrige.Preparat), nesteDato);
    }

    public async Task<Kurdosestatus> LoggDose(
        int dyrId, int behandlingId, int? brukerId, CancellationToken ct)
    {
        var naa = DateTimeOffset.UtcNow;
        var idag = Tidssone.Idag(naa);

        // Norsk midnatt, ikke UTC. Ellers teller kveldsdosen med pa neste
        // dag fram til klokka ett eller to om natten.
        var dagStart = Tidssone.DagStart(naa);

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Query-filteret er autorisasjonen, og den skjer for raden lases -
        // en id fra en annen husstand skal ikke kunne holde en las her.
        var kur = await _db.Behandling
            .Where(b => b.Id == behandlingId && b.DyrId == dyrId)
            .Select(b => new { b.Dato, b.AntallDager, b.GangerPerDag })
            .SingleOrDefaultAsync(ct);

        if (kur is null)
        {
            return Kurdosestatus.FinnesIkke;
        }

        // Sjekkes her og ikke bare ved knappen. En fane som har statt apen
        // siden i gar, viser fortsatt knappen pa siste dag.
        if (!Behandlingskur.ErKur(kur.AntallDager, kur.GangerPerDag)
            || !Behandlingskur.ErAktiv(kur.Dato, kur.AntallDager, idag))
        {
            return Kurdosestatus.IkkeAktiv;
        }

        // Las behandlingsraden for doser telles. To i husstanden som trykker
        // samtidig, ville ellers begge telt 0 av 1 og begge lagt inn en dose.
        // Den andre venter her til den forste har committet, og teller da
        // dosen den forste la inn. Lasen slippes ved commit eller rollback.
        await _db.Database.ExecuteSqlAsync(
            $"SELECT 1 FROM behandling WHERE id = {behandlingId} FOR UPDATE", ct);

        var doserIdag = await _db.BehandlingDose
            .CountAsync(d => d.BehandlingId == behandlingId
                          && d.GittTid >= dagStart, ct);

        if (!Behandlingskur.KanGiDose(
                kur.Dato, kur.AntallDager, kur.GangerPerDag, doserIdag, idag))
        {
            return Kurdosestatus.FullfortIdag;
        }

        _db.BehandlingDose.Add(new BehandlingDose
        {
            BehandlingId = behandlingId,
            GittTid = naa,
            GittAvBrukerId = brukerId
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Kurdosestatus.Lagret;
    }

    public async Task<Kurrad?> HentKurrad(
        int dyrId, int behandlingId, CancellationToken ct)
    {
        var naa = DateTimeOffset.UtcNow;
        var idag = Tidssone.Idag(naa);
        var dagStart = Tidssone.DagStart(naa);

        var kur = await _db.Behandling
            .Where(b => b.Id == behandlingId && b.DyrId == dyrId)
            .Select(b => new
            {
                b.Type,
                b.Preparat,
                b.Dato,
                b.AntallDager,
                b.GangerPerDag,
                DyreNavn = b.Dyr.Navn,
                DoserIdag = b.Doser.Count(d => d.GittTid >= dagStart)
            })
            .SingleOrDefaultAsync(ct);

        if (kur is null || !Behandlingskur.ErKur(kur.AntallDager, kur.GangerPerDag))
        {
            return null;
        }

        return new Kurrad(
            dyrId,
            kur.DyreNavn,
            behandlingId,
            Behandlingsformat.MedPreparat(kur.Type, kur.Preparat),
            Behandlingskur.Dag(kur.Dato, idag),
            kur.AntallDager,
            kur.GangerPerDag,
            kur.DoserIdag,
            Behandlingskur.ErAktiv(kur.Dato, kur.AntallDager, idag));
    }

    private Task<bool> ErApen(int behandlingId, CancellationToken ct)
        => _db.Behandling
            .Where(b => b.Id == behandlingId)
            .AnyAsync(Behandlingsfilter.ApenPaminnelse, ct);

    public async Task<IReadOnlyList<Behandlingsforslag>> HentForslag(
        CancellationToken ct)
    {
        var siste = await _db.Behandling
            .OrderByDescending(b => b.Dato)
            .ThenByDescending(b => b.Id)
            .Take(ForslagGrunnlag)
            .Select(b => new Behandlingsforslag(
                b.Id, b.Type, b.Preparat, b.Dato, b.NesteDato,
                b.AntallDager, b.GangerPerDag))
            .ToListAsync(ct);

        // Grupperes i minnet. Grunnlaget er avgrenset over, og "nyeste rad
        // per gruppe" i SQL ville vaert en vindusfunksjon for et par hundre
        // rader. Samme nokkel som Behandlingsfilter: type og preparat uten
        // hensyn til store og sma bokstaver.
        return siste
            .GroupBy(f => (f.Type, Preparat: f.Preparat?.ToLowerInvariant()))
            .Select(g => g.First())
            .Take(AntallForslag)
            .ToList();
    }

    public async Task<bool> Registrer(Behandlingsinnhold input, CancellationToken ct)
    {
        if (!await _db.Dyr.AnyAsync(d => d.Id == input.DyrId, ct))
        {
            return false;
        }

        _db.Behandling.Add(new Behandling
        {
            DyrId = input.DyrId,
            Type = input.Type,
            Preparat = input.Preparat.TomTilNull(),
            Dato = input.Dato,
            NesteDato = input.NesteDato,
            Notat = input.Notat.TomTilNull(),
            AntallDager = input.AntallDager,
            GangerPerDag = input.GangerPerDag
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Oppdater(
        int behandlingId, Behandlingsinnhold input, CancellationToken ct)
    {
        // Query-filteret er autorisasjonen: en behandling pa et dyr i en
        // annen husstand finnes ikke herfra. DyrId star i tillegg, sa en
        // id fra et annet dyr i EGEN husstand heller ikke treffer.
        var rad = await _db.Behandling
            .SingleOrDefaultAsync(
                b => b.Id == behandlingId && b.DyrId == input.DyrId, ct);

        if (rad is null)
        {
            return false;
        }

        rad.Type = input.Type;
        rad.Preparat = input.Preparat.TomTilNull();
        rad.Dato = input.Dato;
        rad.NesteDato = input.NesteDato;
        rad.Notat = input.Notat.TomTilNull();

        // Doser som allerede er logget, blir staende. Kortes kuren ned,
        // teller de bare ikke lenger - de er fortsatt det som ble gitt.
        rad.AntallDager = input.AntallDager;
        rad.GangerPerDag = input.GangerPerDag;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Slett(
        int dyrId, int behandlingId, CancellationToken ct)
    {
        var rad = await _db.Behandling
            .SingleOrDefaultAsync(
                b => b.Id == behandlingId && b.DyrId == dyrId, ct);

        if (rad is null)
        {
            return false;
        }

        _db.Behandling.Remove(rad);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
