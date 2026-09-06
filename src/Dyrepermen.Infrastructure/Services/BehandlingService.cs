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

    public async Task<IReadOnlyList<BehandlingRad>> HentFor(
        int dyrId, CancellationToken ct)
        => await _db.Behandling
            .Where(b => b.DyrId == dyrId)
            .OrderByDescending(b => b.Dato)
            .ThenByDescending(b => b.Id)
            .Select(b => new BehandlingRad(
                b.Id, b.Type, b.Preparat, b.Dato, b.NesteDato, b.Notat))
            .ToListAsync(ct);

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
