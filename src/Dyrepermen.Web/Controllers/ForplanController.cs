using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Web.Filtre;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

[Route("dyr/{dyrId:int}/forplan")]
public sealed class ForplanController : Controller
{
    private readonly IForplanService _forplan;
    private readonly IDyrService _dyr;

    public ForplanController(IForplanService forplan, IDyrService dyr)
    {
        _forplan = forplan;
        _dyr = dyr;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int dyrId, CancellationToken ct)
    {
        var vm = await ByggSide(dyrId, ny: null, ct);
        return vm is null ? NotFound() : View(vm);
    }

    /// <summary>
    /// Lagrer planen som en NY rad, og lar den gamle sta igjen som
    /// historikk. Brukes nar regelen faktisk endrer seg.
    /// </summary>
    [HttpPost("")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Ny(
        int dyrId, NyForplanVm ny, CancellationToken ct)
        => Lagre(
            dyrId, ny, ct,
            lagre: innhold => _forplan.Opprett(innhold, ct),
            melding: "Fôrplanen er lagret.");

    /// <summary>
    /// Endrer den aktive planen i stedet for a erstatte den. To maltider i
    /// stedet for tre, eller andelen rafor flyttet fra 70 til 60, er ikke en
    /// ny plan - og skal ikke fylle historikken med en rad per uke.
    /// Se ADR 0013.
    /// </summary>
    [HttpPost("rediger")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Rediger(
        int dyrId, NyForplanVm ny, CancellationToken ct)
        => Lagre(
            dyrId, ny, ct,
            lagre: innhold => _forplan.Oppdater(innhold, ct),
            melding: "Fôrplanen er oppdatert.");

    [HttpPost("deaktiver")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deaktiver(int dyrId, CancellationToken ct)
    {
        if (!await ForplanErPa(dyrId, ct) || !await _forplan.Deaktiver(dyrId, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Fôrplanen er deaktivert.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    /// <summary>
    /// De to skrivehandlingene skiller seg pa ett kall og en melding. Resten
    /// - paafyll av tomme rader, validering, omregning til tidels prosent -
    /// er felles, og en kopi per handling ville sprikt ved forste endring.
    /// </summary>
    private async Task<IActionResult> Lagre(
        int dyrId,
        NyForplanVm ny,
        CancellationToken ct,
        Func<Forplaninnhold, Task<bool>> lagre,
        string melding)
    {
        // Funksjonsbryteren styrer visning OG tilgang. Uten sjekken her kan
        // en gammel faneside eller et bokmerke skrive til en avslatt
        // funksjon. Se plan kapittel 8.2.
        if (!await ForplanErPa(dyrId, ct))
        {
            return NotFound();
        }

        // Modellbinderen fyller kun radene som faktisk ble sendt inn. Uten
        // paafyll krasjer visningen pa den forste raden den ikke finner.
        while (ny.Trinn.Count < NyForplanVm.Trinnplasser)
        {
            ny.Trinn.Add(new TrinnradVm());
        }

        if (!ModelState.IsValid)
        {
            var feil = await ByggSide(dyrId, ny, ct);
            return feil is null ? NotFound() : View(nameof(Index), feil);
        }

        var erTabell = ny.Metode == Formetode.Tabell;

        var ok = await lagre(new Forplaninnhold(
            dyrId,
            ny.Metode,
            // Brukeren skriver 5,0 for 5 %. Databasen holder heltall, sa
            // verdien lagres i tidels prosent: 50. En ren tabellplan har
            // ingen prosentsats, og feltet star da tomt.
            ny.Prosent is { } prosent ? (int)Math.Round(prosent * 10) : null,
            ny.Metode == Formetode.Gram ? ny.GramPerDag : null,
            // Tomt felt betyr to maltider. Standarden ligger her og ikke som
            // ferdig utfylt verdi i skjemaet, slik at brukeren slipper a
            // viske ut en toer for a skrive noe annet.
            ny.AntallMaltider ?? 2,
            // Raforets navn hoerer til innblandingen. Blander planen ikke,
            // er det torrforet som baerer navnet.
            ny.Blander ? ny.Fornavn : erTabell ? null : ny.Fornavn,
            ny.Notat,
            // Uten innblanding er andelen null - da er dette en ren
            // tabellplan, og tjenesten nuller ut prosentsatsen.
            erTabell ? (ny.Blander ? ny.VektdelAndel ?? 0 : 0) : null,
            erTabell ? ny.FornavnAlder : null,
            erTabell ? Trinn(ny) : null));

        if (!ok)
        {
            return NotFound();
        }

        TempData["Melding"] = melding;
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    private async Task<bool> ForplanErPa(int dyrId, CancellationToken ct)
        => await _dyr.HentDetaljer(dyrId, ct) is { ForplanAktiv: true };

    private static IReadOnlyList<Alderstrinn> Trinn(NyForplanVm ny)
        => [.. ny.Trinn
            .Where(t => t.ErUtfylt)
            .OrderBy(t => t.AlderMnd)
            .Select(t => new Alderstrinn(t.AlderMnd!.Value, t.GramPerDag!.Value))];

    /// <summary>
    /// <paramref name="ny"/> er null nar skjemaet ikke er sendt inn. Da fylles
    /// det fra den aktive planen: andelen rafor justeres gjennom en overgang
    /// som varer i uker, og a taste inn hele torrfortabellen pa nytt for a
    /// flytte den fra 70 til 60 ville gjort funksjonen ubrukelig.
    /// </summary>
    private async Task<ForplanSideVm?> ByggSide(
        int dyrId, NyForplanVm? ny, CancellationToken ct)
    {
        var dyr = await _dyr.HentDetaljer(dyrId, ct);

        // Funksjonsbryteren styrer visning OG tilgang. Uten sjekken her kan
        // en gammel faneside eller et bokmerke skrive til en avslatt
        // funksjon. Se plan kapittel 8.2.
        if (dyr is null || !dyr.ForplanAktiv)
        {
            return null;
        }

        var resultat = await _forplan.BeregnAktiv(dyrId, ct);
        var aktiv = await _forplan.HentAktiv(dyrId, ct);

        var kanFordele = resultat is { HarPlan: true, ManglerGrunnlag: false };

        return new ForplanSideVm
        {
            DyrId = dyrId,
            DyrNavn = dyr.Navn,
            HarFodselsdato = dyr.Fodselsdato is not null,
            Resultat = resultat,
            Aktiv = aktiv,
            Maltider = kanFordele
                ? Maltidsfordeling.Fordel(resultat.GramPerDag, resultat.AntallMaltider)
                : [],
            VektdelMaltider = kanFordele && resultat.Fordeling is { } rafor
                ? Maltidsfordeling.Fordel(rafor.VektdelGram, resultat.AntallMaltider)
                : [],
            AldersdelMaltider = kanFordele && resultat.Fordeling is { } torr
                ? Maltidsfordeling.Fordel(torr.AldersdelGram, resultat.AntallMaltider)
                : [],
            Fornavnforslag = await _forplan.HentFornavn(ct),
            Ny = ny ?? FraPlan(aktiv)
        };
    }

    private static NyForplanVm FraPlan(ForplanRad? plan)
    {
        var vm = new NyForplanVm();

        if (plan is null)
        {
            return vm;
        }

        vm.Metode = plan.Metode;
        vm.Prosent = plan.ProsentTidels is { } tidels ? tidels / 10m : null;
        vm.GramPerDag = plan.GramPerDag;
        vm.BlandToFor = plan.VektdelAndelProsent > 0;
        vm.VektdelAndel = plan.VektdelAndelProsent is > 0 ? plan.VektdelAndelProsent : null;
        vm.AntallMaltider = plan.AntallMaltider;
        vm.Fornavn = plan.Fornavn;
        vm.FornavnAlder = plan.FornavnAlder;
        vm.Notat = plan.Notat;

        // Flere trinn enn det er plasser skal ikke forsvinne stille, men de
        // kan heller ikke vises. Grensen er den samme i begge ender, sa
        // dette kan bare skje for en plan lagt inn for grensen ble endret.
        foreach (var (trinn, i) in plan.Trinn.Take(NyForplanVm.Trinnplasser)
                     .Select((t, i) => (t, i)))
        {
            vm.Trinn[i].AlderMnd = trinn.AlderMnd;
            vm.Trinn[i].GramPerDag = trinn.GramPerDag;
        }

        return vm;
    }
}
