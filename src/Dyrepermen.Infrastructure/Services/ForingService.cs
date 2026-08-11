using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Infrastructure.Services;

public sealed class ForingService : IForingService
{
    private readonly DyrepermenDbContext _db;

    public ForingService(DyrepermenDbContext db) => _db = db;

    public async Task<IReadOnlyList<ForingRad>> HentFor(
        int dyrId, CancellationToken ct)
        => await _db.Foring
            .Where(f => f.DyrId == dyrId)
            .OrderByDescending(f => f.Tidspunkt)
            .ThenByDescending(f => f.Id)
            .Select(f => new ForingRad(
                f.Id,
                f.Tidspunkt,
                f.MengdeGram,
                // Nullbar fordi brukeren kan vaere slettet.
                f.GittAv == null ? null : f.GittAv.Visningsnavn,
                f.Kommentar))
            .ToListAsync(ct);

    public async Task<bool> Registrer(NyForing input, CancellationToken ct)
    {
        // Bryteren styrer visning OG tilgang. Uten sjekken her kan en gammel
        // faneside eller et bokmerke skrive til en avslatt funksjon.
        // Se plan kapittel 8.2.
        if (!await ForingErPa(input.DyrId, ct))
        {
            return false;
        }

        _db.Foring.Add(new Foring
        {
            DyrId = input.DyrId,
            // Tidspunktet settes HER, pa serveren. Det er ikke en parameter,
            // sa klienten kan ikke pavirke det.
            Tidspunkt = DateTimeOffset.UtcNow,
            MengdeGram = input.MengdeGram,
            Kommentar = input.Kommentar.TomTilNull(),
            GittAvBrukerId = input.GittAvBrukerId
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RedigerTid(
        int dyrId, int foringId, DateTimeOffset tidspunkt, CancellationToken ct)
    {
        if (!await ForingErPa(dyrId, ct))
        {
            return false;
        }

        var rad = await _db.Foring
            .SingleOrDefaultAsync(f => f.Id == foringId && f.DyrId == dyrId, ct);

        if (rad is null)
        {
            return false;
        }

        // Lagres i UTC uansett hva som kommer inn.
        rad.Tidspunkt = tidspunkt.ToUniversalTime();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Slett(int dyrId, int foringId, CancellationToken ct)
    {
        if (!await ForingErPa(dyrId, ct))
        {
            return false;
        }

        var rad = await _db.Foring
            .SingleOrDefaultAsync(f => f.Id == foringId && f.DyrId == dyrId, ct);

        if (rad is null)
        {
            return false;
        }

        _db.Foring.Remove(rad);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Query-filteret gjor eierskapssjekken: et dyr i en annen husstand gir
    /// false. Bryteren sjekkes i samme sporring.
    /// </summary>
    private Task<bool> ForingErPa(int dyrId, CancellationToken ct)
        => _db.Dyr.AnyAsync(d => d.Id == dyrId && d.ForingsloggAktiv, ct);
}
