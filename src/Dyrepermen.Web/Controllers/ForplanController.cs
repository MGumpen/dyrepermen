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
        var vm = await ByggSide(dyrId, new NyForplanVm(), ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost("")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ny(
        int dyrId, NyForplanVm ny, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var vm = await ByggSide(dyrId, ny, ct);
            return vm is null ? NotFound() : View(nameof(Index), vm);
        }

        var erProsent = ny.Metode == Formetode.Prosent;

        var ok = await _forplan.Opprett(new NyForplan(
            dyrId,
            ny.Metode,
            // Brukeren skriver 5,0 for 5 %. Databasen holder heltall, sa
            // verdien lagres i tidels prosent: 50.
            erProsent ? (int)Math.Round(ny.Prosent!.Value * 10) : null,
            erProsent ? null : ny.GramPerDag,
            ny.AntallMaltider,
            ny.Fornavn,
            ny.Notat), ct);

        if (!ok)
        {
            return NotFound();
        }

        TempData["Melding"] = "Fôrplanen er lagret.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    [HttpPost("deaktiver")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deaktiver(int dyrId, CancellationToken ct)
    {
        if (!await _forplan.Deaktiver(dyrId, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Fôrplanen er deaktivert.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    private async Task<ForplanSideVm?> ByggSide(
        int dyrId, NyForplanVm ny, CancellationToken ct)
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

        return new ForplanSideVm
        {
            DyrId = dyrId,
            DyrNavn = dyr.Navn,
            Resultat = resultat,
            Aktiv = await _forplan.HentAktiv(dyrId, ct),
            Maltider = resultat is { HarPlan: true, ManglerVekt: false }
                ? Maltidsfordeling.Fordel(resultat.GramPerDag, resultat.AntallMaltider)
                : [],
            Ny = ny
        };
    }
}
