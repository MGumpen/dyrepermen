using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Web.Extensions;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

[Route("dyr/{dyrId:int}/vekt")]
public sealed class VektController : Controller
{
    private readonly IVektService _vekt;
    private readonly IDyrService _dyr;

    public VektController(IVektService vekt, IDyrService dyr)
    {
        _vekt = vekt;
        _dyr = dyr;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int dyrId, CancellationToken ct)
    {
        var vm = await ByggSide(dyrId, new NyVektVm(), ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrer(
        int dyrId, NyVektVm ny, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var vm = await ByggSide(dyrId, ny, ct);
            return vm is null ? NotFound() : View(nameof(Index), vm);
        }

        var ok = await _vekt.Registrer(
            new NyVekt(dyrId, ny.Kilo, ny.Dato, User.BrukerId()), ct);

        if (!ok)
        {
            return NotFound();
        }

        TempData["Melding"] = "Vekten er registrert.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    [HttpPost("{vektId:int}/slett")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Slett(
        int dyrId, int vektId, CancellationToken ct)
    {
        if (!await _vekt.Slett(dyrId, vektId, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Målingen er slettet.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    private async Task<VektSideVm?> ByggSide(
        int dyrId, NyVektVm ny, CancellationToken ct)
    {
        // Dyret hentes gjennom query-filteret. Et dyr i en annen husstand
        // gir null, og controlleren svarer 404.
        var dyr = await _dyr.HentDetaljer(dyrId, ct);
        if (dyr is null)
        {
            return null;
        }

        return new VektSideVm
        {
            DyrId = dyrId,
            DyrNavn = dyr.Navn,
            Historikk = await _vekt.HentFor(dyrId, ct),
            Ny = ny
        };
    }
}
