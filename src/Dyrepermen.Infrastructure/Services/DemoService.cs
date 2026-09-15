using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dyrepermen.Infrastructure.Services;

/// <summary>
/// Oppretter og rydder demohusstander. Se ADR 0015.
///
/// Ingen egne demostier: demoen er en vanlig bruker med en vanlig husstand,
/// og alt bak innloggingen er den ekte appen med de samme filtrene og
/// rollene.
/// </summary>
public sealed class DemoService : IDemoService
{
    /// <summary>
    /// Hoyst sa mange aktive demoer. Stopper massegenerering fra mange IP-er,
    /// og beskytter lagringen pa Neons gratisniva.
    /// </summary>
    public const int Tak = 300;

    /// <summary>
    /// Utlopte demoer som slettes per nye demo. Ingen planlagt jobb trengs,
    /// og ingen enkelt foresporsel far en ubegrenset sletting.
    /// </summary>
    private const int Ryddeporsjon = 50;

    private static readonly TimeSpan Levetid = TimeSpan.FromHours(24);

    private readonly DyrepermenDbContext _db;
    private readonly UserManager<Bruker> _brukere;
    private readonly ILogger<DemoService> _log;

    public DemoService(
        DyrepermenDbContext db,
        UserManager<Bruker> brukere,
        ILogger<DemoService> log)
    {
        _db = db;
        _brukere = brukere;
        _log = log;
    }

    public async Task<int?> Start(CancellationToken ct)
    {
        var naa = DateTimeOffset.UtcNow;

        await Rydd(naa, ct);

        if (await _db.Users.CountAsync(u => u.DemoUtloper > naa, ct) >= Tak)
        {
            _log.LogWarning("Taket pa {Tak} aktive demoer er nadd", Tak);
            return null;
        }

        // .invalid er et reservert toppdomene og kan aldri motta e-post.
        var epost = $"demo-{Guid.NewGuid():N}@demo.invalid";
        var bruker = new Bruker
        {
            UserName = epost,
            Email = epost,
            Visningsnavn = "Demobruker",
            DemoUtloper = naa + Levetid
        };

        // Brukeren og husstanden lagres sammen eller ikke i det hele tatt.
        // UserManager bruker samme DbContext, og dermed samme transaksjon.
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Uten passord. Demobrukeren logges inn av DemoController, og har
        // ingen innlogging a stjele.
        var opprettet = await _brukere.CreateAsync(bruker);
        if (!opprettet.Succeeded)
        {
            throw new InvalidOperationException(
                "Demobrukeren kunne ikke opprettes: "
                + string.Join(", ", opprettet.Errors.Select(e => e.Code)));
        }

        var mal = Demomal.Bygg(bruker, naa);
        _db.Add(mal.Husstand);
        _db.AddRange(mal.Veterinarer);
        _db.AddRange(mal.Informasjon);
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        _log.LogInformation("Demo startet for {BrukerId}", bruker.Id);
        return bruker.Id;
    }

    public async Task Avslutt(int brukerId, CancellationToken ct)
    {
        // Kun demobrukere. Sjekken star her og ikke bare hos den som kaller,
        // slik at en feil et annet sted aldri kan slette en ekte konto.
        if (!await _db.Users.AnyAsync(u => u.Id == brukerId && u.DemoUtloper != null, ct))
        {
            return;
        }

        await _db.SlettBrukere([brukerId], ct);
        _log.LogInformation("Demo avsluttet for {BrukerId}", brukerId);
    }

    private async Task Rydd(DateTimeOffset naa, CancellationToken ct)
    {
        var utlopte = await _db.Users
            .Where(u => u.DemoUtloper < naa)
            .OrderBy(u => u.DemoUtloper)
            .Take(Ryddeporsjon)
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (utlopte.Count == 0)
        {
            return;
        }

        await _db.SlettBrukere(utlopte, ct);
        _log.LogInformation("Ryddet bort {Antall} utlopte demoer", utlopte.Count);
    }
}
