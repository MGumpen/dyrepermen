using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
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

        // Godbitloggen har sin egen bryter, pa husstandsniva. Samme regel:
        // den skjuler knappen OG stenger endepunktet.
        if (input.Type == Foringstype.Godbit && !await GodbitErPa(ct))
        {
            return false;
        }

        _db.Foring.Add(new Foring
        {
            DyrId = input.DyrId,
            // Tidspunktet settes HER, pa serveren. Det er ikke en parameter,
            // sa klienten kan ikke pavirke det.
            Tidspunkt = DateTimeOffset.UtcNow,
            Type = input.Type,
            MengdeGram = input.MengdeGram,
            Fornavn = input.Fornavn.TomTilNull(),
            Kommentar = input.Kommentar.TomTilNull(),
            GittAvBrukerId = input.GittAvBrukerId
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Navn som allerede er brukt i husstanden, nyeste forst. Grunnlaget for
    /// forslagene i dialogen - da holder stavematen seg stabil uten at noen
    /// ma vedlikeholde et register.
    /// </summary>
    public async Task<IReadOnlyList<string>> HentFornavn(
        Foringstype type, CancellationToken ct)
        => await _db.Foring
            .Where(f => f.Type == type && f.Fornavn != null)
            .GroupBy(f => f.Fornavn!)
            .OrderByDescending(g => g.Max(f => f.Tidspunkt))
            .Select(g => g.Key)
            .Take(15)
            .ToListAsync(ct);

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

    /// <summary>
    /// Mangler innstillingsraden, gjelder standardverdien fra Domain - altsa
    /// pa. En manglende rad skal ikke stenge en funksjon brukeren aldri har
    /// slatt av.
    /// </summary>
    private async Task<bool> GodbitErPa(CancellationToken ct)
        => await _db.HusstandInnstilling
            .Select(i => (bool?)i.GodbitloggAktiv)
            .FirstOrDefaultAsync(ct) ?? true;
}
