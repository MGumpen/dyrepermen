using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Infrastructure.Services;

public sealed class ForsikringService : IForsikringService
{
    private readonly DyrepermenDbContext _db;

    public ForsikringService(DyrepermenDbContext db) => _db = db;

    public async Task<IReadOnlyList<ForsikringRad>> Hent(CancellationToken ct)
        => await Projiser(_db.Forsikring
                .OrderBy(f => f.Dyr.Navn)
                .ThenBy(f => f.Selskap))
            .ToListAsync(ct);

    public async Task<ForsikringRad?> HentEn(int id, CancellationToken ct)
        => await Projiser(_db.Forsikring.Where(f => f.Id == id))
            .SingleOrDefaultAsync(ct);

    private static IQueryable<ForsikringRad> Projiser(IQueryable<Forsikring> q)
        => q.Select(f => new ForsikringRad(
            f.Id, f.DyrId, f.Dyr.Navn, f.Selskap, f.PoliseNr,
            f.ArspremieKr, f.ForsikringsbelopKr,
            f.EgenandelFastKr, f.EgenandelVariabelTidels, f.FornyesDato));

    public async Task<bool> Lagre(NyForsikring input, CancellationToken ct)
    {
        var selskap = input.Selskap.TomTilNull();
        if (selskap is null)
        {
            return false;
        }

        // Polisen ma hore til et dyr i egen husstand. Query-filteret gjor
        // oppslaget, sa en fremmed dyrId gir false.
        if (!await _db.Dyr.AnyAsync(d => d.Id == input.DyrId, ct))
        {
            return false;
        }

        if (input.Id is { } id)
        {
            var rad = await _db.Forsikring.SingleOrDefaultAsync(f => f.Id == id, ct);
            if (rad is null)
            {
                return false;
            }

            Fyll(rad, input, selskap);
        }
        else
        {
            var rad = new Forsikring();
            Fyll(rad, input, selskap);
            _db.Forsikring.Add(rad);
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static void Fyll(Forsikring rad, NyForsikring input, string selskap)
    {
        rad.DyrId = input.DyrId;
        rad.Selskap = selskap;
        rad.PoliseNr = input.PoliseNr.TomTilNull();
        rad.ArspremieKr = input.ArspremieKr;
        rad.ForsikringsbelopKr = input.ForsikringsbelopKr;
        rad.EgenandelFastKr = input.EgenandelFastKr;
        rad.EgenandelVariabelTidels = input.EgenandelVariabelTidels;
        rad.FornyesDato = input.FornyesDato;
    }

    public async Task<bool> Slett(int id, CancellationToken ct)
    {
        var rad = await _db.Forsikring.SingleOrDefaultAsync(f => f.Id == id, ct);
        if (rad is null)
        {
            return false;
        }

        _db.Forsikring.Remove(rad);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
