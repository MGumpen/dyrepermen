using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Infrastructure.Services;

/// <summary>
/// Samler alt om alle dyr i husstanden til utskriftssiden.
///
/// En sporring per tabell, ikke en per dyr. Alternativet - a kalle de
/// eksisterende HentFor(dyrId)-metodene i en lokke - ville gitt seks
/// rundturer GANGE antall dyr. Her grupperes radene i minnet etterpa, og
/// tallet star stille uansett hvor mange dyr husstanden har.
///
/// Query-filtrene gjor husstandsavgrensningen, som ellers i appen. Utvalget
/// av dyr legges oppa i SQL, og en del som ikke er valgt, hentes ikke.
/// </summary>
public sealed class UtskriftService : IUtskriftService
{
    private readonly DyrepermenDbContext _db;
    private readonly IInformasjonService _informasjon;

    public UtskriftService(
        DyrepermenDbContext db, IInformasjonService informasjon)
    {
        _db = db;
        _informasjon = informasjon;
    }

    public async Task<Utskrift> Hent(Utskriftsvalg valg, CancellationToken ct)
    {
        // Null betyr alle dyr. En tom liste betyr ingen - da slipper vi
        // resten av sporringene, men fellesnotatene kan fortsatt vaere valgt.
        var ider = valg.DyrIder;

        // Sporring 1. Dyrene selv. Query-filteret tar bort de deaktiverte, og
        // ider fra en annen husstand.
        var dyr = await _db.Dyr
            .Where(d => ider == null || ider.Contains(d.Id))
            .OrderBy(d => d.Navn)
            .Select(Dyrprojeksjon.Detaljer)
            .ToListAsync(ct);

        // Notatene hentes en gang og deles mellom dyrene og fellesdelen.
        var notater = valg.Har(Utskriftsdel.Notater) || valg.Har(Utskriftsdel.FellesNotater)
            ? await _informasjon.Hent(ct)
            : [];

        var felles = valg.Har(Utskriftsdel.FellesNotater)
            ? notater.Where(n => n.DyrId is null).ToList()
            : [];

        if (dyr.Count == 0)
        {
            return new Utskrift([], felles, valg);
        }

        // Fra her av avgrenses alt til dyrene som faktisk ble funnet.
        var dyrIder = dyr.Select(d => d.Id).ToList();

        // Sporring 2. Alle vekter, nyeste forst - samme rekkefolge som
        // vektsiden bruker.
        var vekter = !valg.Har(Utskriftsdel.Vekt) ? [] : (await _db.Vekt
            .Where(v => dyrIder.Contains(v.DyrId))
            .OrderByDescending(v => v.Dato)
            .ThenByDescending(v => v.Id)
            .Select(v => new
            {
                v.DyrId,
                Rad = new VektRad(
                    v.Id, v.VektGram, v.Dato,
                    // Nullbar fordi brukeren kan vaere slettet.
                    v.RegistrertAv == null ? null : v.RegistrertAv.Visningsnavn)
            })
            .ToListAsync(ct))
            .GroupBy(v => v.DyrId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<VektRad>)
                g.Select(v => v.Rad).ToList());

        // Sporring 3. Behandlinger, nyeste forst.
        var behandlingsrader = !valg.Har(Utskriftsdel.Behandlinger) ? [] : await _db.Behandling
            .Where(b => dyrIder.Contains(b.DyrId))
            .OrderByDescending(b => b.Dato)
            .ThenByDescending(b => b.Id)
            .Select(b => new
            {
                b.DyrId,
                Rad = new BehandlingRad(
                    b.Id, b.Type, b.Preparat, b.Dato, b.NesteDato, b.Notat, false)
            })
            .ToListAsync(ct);

        // Sporring 3b. Hvilke som fortsatt venter. Et ark som sier "neste
        // 1. mars" om en ormekur som ble fulgt opp i februar, sender
        // hundepasseren til dyrlegen for ingenting. Samme regel som
        // dashbordet, se ADR 0016.
        var apne = behandlingsrader.Count == 0 ? [] : (await _db.Behandling
            .Where(b => dyrIder.Contains(b.DyrId))
            .Where(Behandlingsfilter.ApenPaminnelse)
            .Select(b => b.Id)
            .ToListAsync(ct))
            .ToHashSet();

        var behandlinger = behandlingsrader
            .Select(b => new
            {
                b.DyrId,
                Rad = apne.Contains(b.Rad.Id) ? b.Rad with { ErApen = true } : b.Rad
            })
            .GroupBy(b => b.DyrId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<BehandlingRad>)
                g.Select(b => b.Rad).ToList());

        // Sporring 4. Medisiner. Siste dose hentes som korrelert
        // undersporring, i samme rundtur.
        var medisiner = !valg.Har(Utskriftsdel.Medisiner) ? [] : (await _db.Medisin
            .Where(m => dyrIder.Contains(m.DyrId))
            .OrderBy(m => m.Navn)
            .Select(m => new
            {
                m.DyrId,
                Rad = new MedisinRad(
                    m.Id, m.Navn, m.Dose, m.IntervallTimer,
                    m.StartDato, m.SluttDato,
                    m.Doser.OrderByDescending(d => d.GittTid)
                        .Select(d => (DateTimeOffset?)d.GittTid)
                        .FirstOrDefault(),
                    m.Doser.OrderByDescending(d => d.GittTid)
                        .Select(d => d.GittAv == null
                            ? null : d.GittAv.Visningsnavn)
                        .FirstOrDefault(),
                    m.AvsluttetTid)
            })
            .ToListAsync(ct))
            .GroupBy(m => m.DyrId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<MedisinRad>)
                g.Select(m => m.Rad).ToList());

        // Sporring 5. Kun den aktive forplanen per dyr.
        var forplaner = !valg.Har(Utskriftsdel.Forplan) ? [] : await _db.Forplan
            .Where(f => f.Aktiv && dyrIder.Contains(f.DyrId))
            .Select(f => new
            {
                f.DyrId,
                Rad = new ForplanRad(
                    f.Id, f.Metode, f.ProsentTidels, f.GramPerDag,
                    f.AntallMaltider, f.Fornavn, f.Notat, f.OpprettetDato,
                    f.EndretDato,
                    // Uttrykkstre - ingen valgfrie parametere.
                    f.VektdelAndelProsent, f.FornavnAlder, null)
            })
            .ToListAsync(ct);

        // Sporring 5b. Torrfortabellene, kun nar en blandingsplan finnes.
        // Utskriften skal kunne tas med til hundepasseren, og da ma tabellen
        // sta der - ikke bare dagens tall.
        if (forplaner.Any(f => f.Rad.Metode == Formetode.Tabell))
        {
            var trinn = (await _db.Forplantrinn
                .Where(t => t.Forplan.Aktiv && dyrIder.Contains(t.Forplan.DyrId))
                .OrderBy(t => t.AlderMnd)
                .Select(t => new { t.ForplanId, t.AlderMnd, t.GramPerDag })
                .ToListAsync(ct))
                .GroupBy(t => t.ForplanId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<Alderstrinn>)g
                        .Select(t => new Alderstrinn(t.AlderMnd, t.GramPerDag))
                        .ToList());

            forplaner = forplaner
                .Select(f => new
                {
                    f.DyrId,
                    Rad = f.Rad with
                    {
                        Tabelltrinn = trinn.GetValueOrDefault(f.Rad.Id, [])
                    }
                })
                .ToList();
        }

        // Sporring 6. Forsikringer.
        var forsikringer = !valg.Har(Utskriftsdel.Forsikring) ? [] : (await _db.Forsikring
            .Where(f => dyrIder.Contains(f.DyrId))
            .OrderBy(f => f.Selskap)
            .Select(f => new
            {
                f.DyrId,
                Rad = new ForsikringRad(
                    f.Id, f.DyrId, f.Dyr.Navn, f.Selskap, f.PoliseNr,
                    f.ArspremieKr, f.ForsikringsbelopKr, f.EgenandelFastKr,
                    f.EgenandelVariabelTidels, f.FornyesDato)
            })
            .ToListAsync(ct))
            .GroupBy(f => f.DyrId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ForsikringRad>)
                g.Select(f => f.Rad).ToList());

        var sider = dyr.Select(d =>
        {
            var vekt = vekter.GetValueOrDefault(d.Id, []);

            return new DyrUtskrift(
                d,
                vekt,
                // Grafen regnes ut av samme kode som vektsiden bruker, sa
                // arket og skjermen aldri kan vise ulik kurve. Beregn vil ha
                // stigende dato; listen over er synkende.
                Vektgrafberegning.Beregn(
                    vekt.Reverse().Select(v => (v.Dato, v.VektGram)).ToList()),
                behandlinger.GetValueOrDefault(d.Id, []),
                medisiner.GetValueOrDefault(d.Id, []),
                forplaner.SingleOrDefault(f => f.DyrId == d.Id)?.Rad,
                forsikringer.GetValueOrDefault(d.Id, []),
                valg.Har(Utskriftsdel.Notater)
                    ? notater.Where(n => n.DyrId == d.Id).ToList()
                    : []);
        }).ToList();

        return new Utskrift(sider, felles, valg);
    }
}
