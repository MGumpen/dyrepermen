using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Infrastructure.Services;

public sealed class HandlelisteService : IHandlelisteService
{
    private readonly DyrepermenDbContext _db;
    private readonly IHusstandContext _husstand;

    public HandlelisteService(DyrepermenDbContext db, IHusstandContext husstand)
    {
        _db = db;
        _husstand = husstand;
    }

    private IQueryable<HandlelisteRad> Projiser(IQueryable<Handleliste> q)
        => q.Select(h => new HandlelisteRad(
            h.Id,
            h.Tekst,
            h.Antall,
            h.Status,
            // Null her blir "Felles" i visningen. Punktet henger pa
            // husstanden, ikke pa dyret - koblingen er valgfri.
            h.Dyr == null ? null : h.Dyr.Navn,
            h.OpprettetAv == null ? null : h.OpprettetAv.Visningsnavn,
            h.OpprettetDato));

    public async Task<IReadOnlyList<HandlelisteRad>> Hent(CancellationToken ct)
        => await Projiser(_db.Handleliste
                .OrderBy(h => h.Status == HandlelisteStatus.Kjopt)
                .ThenBy(h => h.OpprettetDato)
                .ThenBy(h => h.Id))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<HandlelisteRad>> HentAktive(
        int antall, CancellationToken ct)
        => await Projiser(_db.Handleliste
                .Where(h => h.Status == HandlelisteStatus.Aktiv)
                .OrderBy(h => h.OpprettetDato)
                .ThenBy(h => h.Id)
                .Take(antall))
            .ToListAsync(ct);

    public async Task<bool> Legg(NyttPunkt input, CancellationToken ct)
    {
        var tekst = input.Tekst.TomTilNull();
        if (tekst is null)
        {
            return false;
        }

        // Er punktet knyttet til et dyr, ma dyret tilhore husstanden.
        // Query-filteret gjor oppslaget, sa en fremmed dyrId gir false.
        if (input.DyrId is { } dyrId
            && !await _db.Dyr.AnyAsync(d => d.Id == dyrId, ct))
        {
            return false;
        }

        _db.Handleliste.Add(new Handleliste
        {
            HusstandId = _husstand.HusstandId,
            Tekst = tekst,
            Antall = Math.Max(input.Antall, 1),
            DyrId = input.DyrId,
            Status = HandlelisteStatus.Aktiv,
            OpprettetAvBrukerId = input.OpprettetAvBrukerId
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> VekslStatus(int punktId, CancellationToken ct)
    {
        var punkt = await _db.Handleliste
            .SingleOrDefaultAsync(h => h.Id == punktId, ct);

        if (punkt is null)
        {
            return false;
        }

        punkt.Status = punkt.Status == HandlelisteStatus.Aktiv
            ? HandlelisteStatus.Kjopt
            : HandlelisteStatus.Aktiv;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Slett(int punktId, CancellationToken ct)
    {
        var punkt = await _db.Handleliste
            .SingleOrDefaultAsync(h => h.Id == punktId, ct);

        if (punkt is null)
        {
            return false;
        }

        _db.Handleliste.Remove(punkt);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> RyddKjopte(CancellationToken ct)
        // Query-filteret gjelder ogsa for ExecuteDelete, sa dette rammer kun
        // egen husstand.
        => await _db.Handleliste
            .Where(h => h.Status == HandlelisteStatus.Kjopt)
            .ExecuteDeleteAsync(ct);
}
