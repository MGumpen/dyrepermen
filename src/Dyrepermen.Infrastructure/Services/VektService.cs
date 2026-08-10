using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Infrastructure.Services;

public sealed class VektService : IVektService
{
    private readonly DyrepermenDbContext _db;

    public VektService(DyrepermenDbContext db) => _db = db;

    public async Task<IReadOnlyList<VektRad>> HentFor(
        int dyrId, CancellationToken ct)
        => await _db.Vekt
            .Where(v => v.DyrId == dyrId)
            // Synkende dato, deretter synkende Id. Uten Id-en er rekkefolgen
            // vilkarlig nar to malinger har samme dato.
            .OrderByDescending(v => v.Dato)
            .ThenByDescending(v => v.Id)
            .Select(v => new VektRad(
                v.Id,
                v.VektGram,
                v.Dato,
                // Nullbar fordi brukeren kan vaere slettet. Visningslaget
                // skriver "slettet bruker" der navnet sto.
                v.RegistrertAv == null ? null : v.RegistrertAv.Visningsnavn))
            .ToListAsync(ct);

    public async Task<bool> Registrer(NyVekt input, CancellationToken ct)
    {
        // Eksplisitt eierskapssjekk for skriving. Query-filteret hindrer at
        // man LESER andres data, men en POST med fremmed dyrId ma avvises
        // her. Se plan kapittel 15.
        if (!await _db.Dyr.AnyAsync(d => d.Id == input.DyrId, ct))
        {
            return false;
        }

        _db.Vekt.Add(new Vekt
        {
            DyrId = input.DyrId,
            VektGram = Vektformat.TilGram(input.Kilo),
            Dato = input.Dato,
            RegistrertAvBrukerId = input.RegistrertAvBrukerId
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Slett(int dyrId, int vektId, CancellationToken ct)
    {
        var rad = await _db.Vekt
            .SingleOrDefaultAsync(v => v.Id == vektId && v.DyrId == dyrId, ct);

        if (rad is null)
        {
            return false;
        }

        _db.Vekt.Remove(rad);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
