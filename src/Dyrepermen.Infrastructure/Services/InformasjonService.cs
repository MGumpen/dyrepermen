using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Infrastructure.Services;

public sealed class InformasjonService : IInformasjonService
{
    private readonly DyrepermenDbContext _db;
    private readonly IHusstandContext _husstand;

    public InformasjonService(DyrepermenDbContext db, IHusstandContext husstand)
    {
        _db = db;
        _husstand = husstand;
    }

    public async Task<IReadOnlyList<InformasjonRad>> Hent(CancellationToken ct)
        => await _db.Informasjon
            // Felles forst, deretter per dyr. Innenfor hver gruppe alfabetisk.
            .OrderBy(i => i.DyrId != null)
            .ThenBy(i => i.Dyr!.Navn)
            .ThenBy(i => i.Tittel)
            .Select(i => new InformasjonRad(
                i.Id, i.Tittel, i.Tekst, i.DyrId,
                i.Dyr == null ? null : i.Dyr.Navn, i.OpprettetDato))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<InformasjonRad>> HentNyeste(
        int antall, CancellationToken ct)
        => await _db.Informasjon
            .OrderByDescending(i => i.OpprettetDato)
            .ThenByDescending(i => i.Id)
            .Take(antall)
            .Select(i => new InformasjonRad(
                i.Id, i.Tittel, i.Tekst, i.DyrId,
                i.Dyr == null ? null : i.Dyr.Navn, i.OpprettetDato))
            .ToListAsync(ct);

    public async Task<InformasjonRad?> HentEn(int id, CancellationToken ct)
        => await _db.Informasjon
            .Where(i => i.Id == id)
            .Select(i => new InformasjonRad(
                i.Id, i.Tittel, i.Tekst, i.DyrId,
                i.Dyr == null ? null : i.Dyr.Navn, i.OpprettetDato))
            .SingleOrDefaultAsync(ct);

    public async Task<bool> Lagre(NyInformasjon input, CancellationToken ct)
    {
        var tittel = input.Tittel.TomTilNull();
        var tekst = input.Tekst.TomTilNull();

        if (tittel is null || tekst is null)
        {
            return false;
        }

        // Er notatet knyttet til et dyr, ma dyret tilhore husstanden.
        // Query-filteret gjor oppslaget.
        if (input.DyrId is { } dyrId
            && !await _db.Dyr.AnyAsync(d => d.Id == dyrId, ct))
        {
            return false;
        }

        if (input.Id is { } id)
        {
            var rad = await _db.Informasjon.SingleOrDefaultAsync(i => i.Id == id, ct);
            if (rad is null)
            {
                return false;
            }

            rad.Tittel = tittel;
            rad.Tekst = tekst;
            rad.DyrId = input.DyrId;
        }
        else
        {
            _db.Informasjon.Add(new Informasjon
            {
                HusstandId = _husstand.HusstandId,
                Tittel = tittel,
                Tekst = tekst,
                DyrId = input.DyrId,
                OpprettetAvBrukerId = input.OpprettetAvBrukerId
            });
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Slett(int id, CancellationToken ct)
    {
        var rad = await _db.Informasjon.SingleOrDefaultAsync(i => i.Id == id, ct);
        if (rad is null)
        {
            return false;
        }

        _db.Informasjon.Remove(rad);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<DyreOversikt>> HentDyreoversikt(
        CancellationToken ct)
    {
        var idag = Tidssone.Idag(DateTimeOffset.UtcNow);

        // Korrelerte undersporringer, ikke Include etterfulgt av filtrering i
        // C#. Hele oversikten kommer i en rundtur uansett antall dyr.
        var dyr = await _db.Dyr
            .OrderBy(d => d.Navn)
            .Select(d => new
            {
                d.Id,
                d.Navn,
                d.Art,
                d.Kjonn,
                d.Rase,
                d.Fodselsdato,
                d.ChipNr,
                d.RegNrNkk,
                d.Kastrert,
                SisteVekt = d.Vekter
                    .OrderByDescending(v => v.Dato)
                    .ThenByDescending(v => v.Id)
                    .Select(v => new { v.VektGram, v.Dato })
                    .FirstOrDefault(),
                Medisiner = d.Medisiner
                    .Where(m => m.SluttDato == null || m.SluttDato >= idag)
                    .OrderBy(m => m.Navn)
                    .Select(m => m.Navn + " – " + m.Dose)
                    .ToList(),
                Forplan = d.Forplaner
                    .Where(f => f.Aktiv)
                    .Select(f => new
                    {
                        f.Id,
                        f.Metode,
                        f.ProsentTidels,
                        f.GramPerDag,
                        f.AntallMaltider,
                        f.VektdelAndelProsent
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var notater = await Hent(ct);

        // Torrfortabellene, og kun nar noen faktisk har en blandingsplan.
        var trinn = dyr.Any(d => d.Forplan?.Metode == Formetode.Tabell)
            ? (await _db.Forplantrinn
                .Where(t => t.Forplan.Aktiv)
                .OrderBy(t => t.AlderMnd)
                .Select(t => new { t.ForplanId, t.AlderMnd, t.GramPerDag })
                .ToListAsync(ct))
                .GroupBy(t => t.ForplanId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<Alderstrinn>)g
                        .Select(t => new Alderstrinn(t.AlderMnd, t.GramPerDag))
                        .ToList())
            : new Dictionary<int, IReadOnlyList<Alderstrinn>>();

        return dyr.Select(d =>
        {
            var regel = d.Forplan is null ? null : new Forplanregel(
                d.Forplan.Metode,
                d.Forplan.ProsentTidels,
                d.Forplan.GramPerDag,
                d.Forplan.AntallMaltider,
                d.Forplan.VektdelAndelProsent,
                trinn.GetValueOrDefault(d.Forplan.Id, []));

            // Regelen regnes ETT sted og settes i ord ETT sted. Denne
            // tjenesten regnet den for selv, gjennom en Sammendrag-metode
            // som hadde sin egen kopi av prosentregelen.
            var mengde = Forberegning.Beregn(regel, new Beregningsgrunnlag(
                d.SisteVekt?.VektGram,
                d.SisteVekt?.Dato,
                d.Fodselsdato,
                idag));

            return new DyreOversikt(
                d.Id, d.Navn, d.Art, d.Kjonn, d.Rase, d.Fodselsdato,
                d.ChipNr, d.RegNrNkk, d.Kastrert,
                d.SisteVekt?.VektGram,
                d.SisteVekt?.Dato,
                d.Medisiner,
                Forplanformat.Sammendrag(regel, mengde),
                notater.Where(n => n.DyrId == d.Id).ToList());
        }).ToList();
    }
}
