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
                b.Id, b.Type, b.Preparat, b.Dato, b.NesteDato, b.Notat, false))
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

    public async Task<GittResultat> Gitt(
        int dyrId, int behandlingId, CancellationToken ct)
    {
        // Query-filteret er autorisasjonen, DyrId hindrer at en id fra et
        // annet dyr i egen husstand treffer. Samme monster som Oppdater.
        var forrige = await _db.Behandling
            .Where(b => b.Id == behandlingId && b.DyrId == dyrId)
            .Select(b => new { b.Type, b.Preparat, b.Dato, b.NesteDato })
            .SingleOrDefaultAsync(ct);

        if (forrige is null)
        {
            return GittResultat.FinnesIkke();
        }

        // En rad som allerede er fulgt opp, skal ikke gi en behandling til.
        // Uten sjekken ville et dobbelttrykk, eller to i husstanden som
        // krysser av samtidig fra hver sin telefon, registrert ormekuren to
        // ganger.
        var apen = await _db.Behandling
            .Where(b => b.Id == behandlingId)
            .AnyAsync(Behandlingsfilter.ApenPaminnelse, ct);

        if (!apen)
        {
            return GittResultat.AlleredeFulgtOpp();
        }

        var idag = Tidssone.Idag(DateTimeOffset.UtcNow);
        var neste = Behandlingsintervall.NesteEtter(
            forrige.Dato, forrige.NesteDato, idag);

        _db.Behandling.Add(new Behandling
        {
            DyrId = dyrId,
            Type = forrige.Type,
            // Preparatet kopieres som det sto. Det er det som gjor at den
            // nye raden gjenkjennes som oppfolgingen av den gamle.
            Preparat = forrige.Preparat,
            Dato = idag,
            NesteDato = neste
        });

        await _db.SaveChangesAsync(ct);

        return GittResultat.Lagret(
            Behandlingsformat.MedPreparat(forrige.Type, forrige.Preparat), neste);
    }

    public async Task<IReadOnlyList<Behandlingsforslag>> HentForslag(
        CancellationToken ct)
    {
        var siste = await _db.Behandling
            .OrderByDescending(b => b.Dato)
            .ThenByDescending(b => b.Id)
            .Take(ForslagGrunnlag)
            .Select(b => new Behandlingsforslag(
                b.Id, b.Type, b.Preparat, b.Dato, b.NesteDato))
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
            Notat = input.Notat.TomTilNull()
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
