using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Infrastructure.Services;

public sealed class DashbordService : IDashbordService
{
    private const int Varselvindu = 14;

    private const int AntallPaHandleliste = 5;

    private readonly DyrepermenDbContext _db;
    private readonly IHandlelisteService _handleliste;

    public DashbordService(
        DyrepermenDbContext db, IHandlelisteService handleliste)
    {
        _db = db;
        _handleliste = handleliste;
    }

    public async Task<Dashbord> Hent(CancellationToken ct)
    {
        var idag = DateOnly.FromDateTime(DateTime.UtcNow);
        var grense = idag.AddDays(Varselvindu);

        // Sporring 1. Siste vekt hentes som korrelert undersporring inne i
        // Select. Npgsql oversetter den til LEFT JOIN LATERAL og kjorer alt i
        // samme rundtur - ogsa med tjue dyr.
        //
        // Bruk aldri Include etterfulgt av .Last() i C#: da hentes hele
        // vekthistorikken for hvert dyr, og kravet om fire sporringer ryker
        // pa den forste. Se plan kapittel 10.3.
        var dyr = await _db.Dyr
            .OrderBy(d => d.Navn)
            .Select(d => new DyrKort(
                d.Id,
                d.Navn,
                d.Art,
                d.BildeFilnavn,
                d.Fodselsdato,
                d.ForingsloggAktiv,
                d.Vekter
                    .OrderByDescending(v => v.Dato)
                    .ThenByDescending(v => v.Id)
                    .Select(v => (int?)v.VektGram)
                    .FirstOrDefault(),
                d.Vekter
                    .OrderByDescending(v => v.Dato)
                    .ThenByDescending(v => v.Id)
                    .Select(v => (DateOnly?)v.Dato)
                    .FirstOrDefault()))
            .ToListAsync(ct);

        // Sporring 2. Behandlinger som forfaller innen vinduet.
        //
        // Medisiner (gjentas per time, ikke per dato) og forsikring kommer i
        // fase 3 og 5. Kilde-typen dekker dem allerede, sa de kan legges til
        // uten a endre grensesnittet.
        var raa = await _db.Behandling
            .Where(b => b.NesteDato != null && b.NesteDato <= grense)
            .OrderBy(b => b.NesteDato)
            .Select(b => new
            {
                DyreNavn = b.Dyr.Navn,
                b.Type,
                b.Preparat,
                Dato = b.NesteDato!.Value
            })
            .ToListAsync(ct);

        // Teksten bygges etter materialisering. En switch over enum lar seg
        // ikke oversette til SQL, og det er unodvendig a prove.
        var forfaller = raa
            .Select(b => new Paminnelse(
                b.DyreNavn,
                Kilde.Behandling,
                b.Preparat is null
                    ? TypeTekst(b.Type)
                    : $"{TypeTekst(b.Type)} – {b.Preparat}",
                b.Dato))
            .ToList();

        // Sporring 3. De fem oeverste aktive punktene.
        var handleliste = await _handleliste.HentAktive(AntallPaHandleliste, ct);

        // Tre sporringer totalt, uansett antall dyr. Kravet er hoyst fire.
        return new Dashbord(dyr, forfaller, handleliste);
    }

    private static string TypeTekst(BehandlingType type) => type switch
    {
        BehandlingType.Vaksine => "Vaksine",
        BehandlingType.Ormekur => "Ormekur",
        BehandlingType.Flatt => "Flåttmiddel",
        BehandlingType.Kloklipp => "Kloklipp",
        BehandlingType.Tannrens => "Tannrens",
        _ => "Behandling"
    };
}
