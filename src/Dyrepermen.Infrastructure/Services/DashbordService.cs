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

        // Sporring 1. Siste vekt, aktiv forplan, neste behandling og aktive
        // medisiner hentes som korrelerte undersporringer inne i Select.
        // Npgsql oversetter dem til LEFT JOIN LATERAL og kjorer alt i samme
        // rundtur - ogsa med tjue dyr.
        //
        // Bruk aldri Include etterfulgt av .Last() i C#: da hentes hele
        // vekthistorikken for hvert dyr, og kravet om fire sporringer ryker
        // pa den forste. Se plan kapittel 10.3.
        var raa = await _db.Dyr
            .OrderBy(d => d.Navn)
            .Select(d => new
            {
                d.Id,
                d.Navn,
                d.Art,
                d.BildeFilnavn,
                d.Fodselsdato,
                d.ForingsloggAktiv,
                d.ForplanAktiv,

                SisteVekt = d.Vekter
                    .OrderByDescending(v => v.Dato)
                    .ThenByDescending(v => v.Id)
                    .Select(v => new { v.VektGram, v.Dato })
                    .FirstOrDefault(),

                Forplan = d.Forplaner
                    .Where(f => f.Aktiv)
                    .Select(f => new
                    {
                        f.Metode,
                        f.ProsentTidels,
                        f.GramPerDag,
                        f.AntallMaltider
                    })
                    .FirstOrDefault(),

                Neste = d.Behandlinger
                    .Where(b => b.NesteDato != null)
                    .OrderBy(b => b.NesteDato)
                    .Select(b => new { b.Type, b.Preparat, Dato = b.NesteDato!.Value })
                    .FirstOrDefault(),

                Medisiner = d.Medisiner
                    .Where(m => m.SluttDato == null || m.SluttDato >= idag)
                    .OrderBy(m => m.Navn)
                    .Select(m => m.Navn)
                    .ToList()
            })
            .ToListAsync(ct);

        var dyr = raa.Select(d => new DyrKort(
            d.Id,
            d.Navn,
            d.Art,
            d.BildeFilnavn,
            d.Fodselsdato,
            d.ForingsloggAktiv,
            d.SisteVekt?.VektGram,
            d.SisteVekt?.Dato,
            // Bryteren styrer visning: er forplan slatt av for dyret, skal
            // den ikke dukke opp pa kortet heller.
            d.ForplanAktiv
                ? Forplantekst(
                    d.Forplan?.Metode, d.Forplan?.ProsentTidels,
                    d.Forplan?.GramPerDag, d.Forplan?.AntallMaltider,
                    d.SisteVekt?.VektGram)
                : null,
            d.Neste is null ? null : TypeTekst(d.Neste.Type, d.Neste.Preparat),
            d.Neste?.Dato,
            d.Medisiner))
            .ToList();

        // Sporring 2. Behandlinger som forfaller innen vinduet.
        //
        // Medisiner (gjentas per time, ikke per dato) og forsikring kommer i
        // fase 3 og 5. Kilde-typen dekker dem allerede, sa de kan legges til
        // uten a endre grensesnittet.
        var forfallerRaa = await _db.Behandling
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

        // Sporring 3. Forsikringer som skal fornyes innen vinduet.
        var forsikringer = await _db.Forsikring
            .Where(f => f.FornyesDato != null && f.FornyesDato <= grense)
            .OrderBy(f => f.FornyesDato)
            .Select(f => new
            {
                DyreNavn = f.Dyr.Navn,
                f.Selskap,
                Dato = f.FornyesDato!.Value
            })
            .ToListAsync(ct);

        // Teksten bygges etter materialisering. En switch over enum lar seg
        // ikke oversette til SQL, og det er unodvendig a prove.
        var forfaller = forfallerRaa
            .Select(b => new Paminnelse(
                b.DyreNavn,
                Kilde.Behandling,
                TypeTekst(b.Type, b.Preparat),
                b.Dato))
            .Concat(forsikringer.Select(f => new Paminnelse(
                f.DyreNavn,
                Kilde.Forsikring,
                $"Fornyelse {f.Selskap}",
                f.Dato)))
            // Sortert stigende gir forfalte forst - de har eldst dato.
            .OrderBy(p => p.Dato)
            .ToList();

        // Sporring 4. De fem oeverste aktive punktene pa handlelisten.
        var handleliste = await _handleliste.HentAktive(AntallPaHandleliste, ct);

        // Fire sporringer totalt, uansett antall dyr. Det er taket i
        // kapittel 16 - flere kilder ma slas sammen med de eksisterende.
        return new Dashbord(dyr, forfaller, handleliste);
    }

    private static string TypeTekst(BehandlingType type, string? preparat)
    {
        var navn = type switch
        {
            BehandlingType.Vaksine => "Vaksine",
            BehandlingType.Ormekur => "Ormekur",
            BehandlingType.Flatt => "Flåttmiddel",
            BehandlingType.Kloklipp => "Kloklipp",
            BehandlingType.Tannrens => "Tannrens",
            _ => "Behandling"
        };

        return preparat is null ? navn : $"{navn} – {preparat}";
    }

    /// <summary>
    /// Samme regel som ForplanService. Uten vektgrunnlag sier den fra framfor
    /// a vise 0 gram - et tall uten dekning er verre enn ingen tall.
    /// </summary>
    private static string? Forplantekst(
        Formetode? metode, int? prosentTidels, int? gramPerDag,
        int? antallMaltider, int? sisteVektGram)
    {
        if (metode is null)
        {
            return null;
        }

        var maltider = antallMaltider ?? 2;

        if (metode == Formetode.Gram)
        {
            return $"{gramPerDag} g på {maltider} måltider";
        }

        if (sisteVektGram is null)
        {
            return "Mangler vekt";
        }

        var gram = (int)Math.Round(
            sisteVektGram.Value * prosentTidels!.Value / 1000.0,
            MidpointRounding.AwayFromZero);

        return $"{gram} g på {maltider} måltider";
    }
}
