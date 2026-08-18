using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
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
/// Query-filtrene gjor husstandsavgrensningen, som ellers i appen.
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

    public async Task<Utskrift> Hent(CancellationToken ct)
    {
        // Sporring 1. Dyrene selv. Query-filteret tar bort de deaktiverte.
        var dyr = await _db.Dyr
            .OrderBy(d => d.Navn)
            .Select(d => new DyrDetaljer(
                d.Id, d.Navn, d.Art, d.Kjonn, d.Rase, d.Fodselsdato,
                d.ChipNr, d.RegNrNkk, d.Kastrert,
                d.ForingsloggAktiv, d.ForplanAktiv))
            .ToListAsync(ct);

        if (dyr.Count == 0)
        {
            return new Utskrift([], await FellesNotater(ct));
        }

        // Sporring 2. Alle vekter, nyeste forst - samme rekkefolge som
        // vektsiden bruker.
        var vekter = (await _db.Vekt
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
        var behandlinger = (await _db.Behandling
            .OrderByDescending(b => b.Dato)
            .Select(b => new
            {
                b.DyrId,
                Rad = new BehandlingRad(
                    b.Id, b.Type, b.Preparat, b.Dato, b.NesteDato, b.Notat)
            })
            .ToListAsync(ct))
            .GroupBy(b => b.DyrId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<BehandlingRad>)
                g.Select(b => b.Rad).ToList());

        // Sporring 4. Medisiner. Siste dose hentes som korrelert
        // undersporring, i samme rundtur.
        var medisiner = (await _db.Medisin
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
                        .FirstOrDefault())
            })
            .ToListAsync(ct))
            .GroupBy(m => m.DyrId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<MedisinRad>)
                g.Select(m => m.Rad).ToList());

        // Sporring 5. Kun den aktive forplanen per dyr.
        var forplaner = await _db.Forplan
            .Where(f => f.Aktiv)
            .Select(f => new
            {
                f.DyrId,
                Rad = new ForplanRad(
                    f.Id, f.Metode, f.ProsentTidels, f.GramPerDag,
                    f.AntallMaltider, f.Fornavn, f.Notat, f.OpprettetDato)
            })
            .ToListAsync(ct);

        // Sporring 6. Forsikringer.
        var forsikringer = (await _db.Forsikring
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

        // Sporring 7. Notatene, gjennom den eksisterende tjenesten.
        var notater = await _informasjon.Hent(ct);

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
                notater.Where(n => n.DyrId == d.Id).ToList());
        }).ToList();

        return new Utskrift(sider, notater.Where(n => n.DyrId is null).ToList());
    }

    private async Task<IReadOnlyList<InformasjonRad>> FellesNotater(
        CancellationToken ct)
        => (await _informasjon.Hent(ct)).Where(n => n.DyrId is null).ToList();
}
