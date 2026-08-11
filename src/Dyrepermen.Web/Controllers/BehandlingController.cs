using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Web.Filtre;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

[Route("dyr/{dyrId:int}/behandling")]
public sealed class BehandlingController : Controller
{
    private readonly IBehandlingService _behandling;
    private readonly IDyrService _dyr;

    public BehandlingController(IBehandlingService behandling, IDyrService dyr)
    {
        _behandling = behandling;
        _dyr = dyr;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int dyrId, CancellationToken ct)
    {
        var vm = await ByggSide(dyrId, new NyBehandlingVm(), ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost("")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ny(
        int dyrId, NyBehandlingVm ny, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var vm = await ByggSide(dyrId, ny, ct);
            return vm is null ? NotFound() : View(nameof(Index), vm);
        }

        var ok = await _behandling.Registrer(
            new NyBehandling(
                dyrId, ny.Type, ny.Preparat, ny.Dato, ny.NesteDato, ny.Notat),
            ct);

        if (!ok)
        {
            return NotFound();
        }

        TempData["Melding"] = "Behandlingen er registrert.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    [HttpPost("{behandlingId:int}/slett")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Slett(
        int dyrId, int behandlingId, CancellationToken ct)
    {
        if (!await _behandling.Slett(dyrId, behandlingId, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Behandlingen er slettet.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    private async Task<BehandlingSideVm?> ByggSide(
        int dyrId, NyBehandlingVm ny, CancellationToken ct)
    {
        var dyr = await _dyr.HentDetaljer(dyrId, ct);
        if (dyr is null)
        {
            return null;
        }

        return new BehandlingSideVm
        {
            DyrId = dyrId,
            DyrNavn = dyr.Navn,
            Historikk = await _behandling.HentFor(dyrId, ct),
            Ny = ny
        };
    }
}
