using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Web.Extensions;
using Dyrepermen.Web.Filtre;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

[Route("dyr/{dyrId:int}/medisin")]
public sealed class MedisinController : Controller
{
    private readonly IMedisinService _medisin;
    private readonly IDyrService _dyr;

    public MedisinController(IMedisinService medisin, IDyrService dyr)
    {
        _medisin = medisin;
        _dyr = dyr;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int dyrId, CancellationToken ct)
    {
        var vm = await ByggSide(dyrId, new NyMedisinVm(), ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost("")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ny(
        int dyrId, NyMedisinVm ny, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var vm = await ByggSide(dyrId, ny, ct);
            return vm is null ? NotFound() : View(nameof(Index), vm);
        }

        // Tomt intervall betyr 0 - ingen fast gjentakelse.
        var ok = await _medisin.Registrer(new NyMedisin(
            dyrId, ny.Navn, ny.Dose, ny.IntervallTimer ?? 0,
            ny.StartDato, ny.SluttDato), ct);

        if (!ok)
        {
            return NotFound();
        }

        TempData["Melding"] = "Medisinen er lagt til.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    [HttpPost("{medisinId:int}/dose")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoggDose(
        int dyrId, int medisinId, bool bekreft, CancellationToken ct)
    {
        var resultat = await _medisin.LoggDose(
            dyrId, medisinId, User.BrukerId(), bekreft, ct);

        if (resultat.KreverBekreftelse)
        {
            // Ikke en feil - en advarsel. Dosen er ikke logget, og brukeren
            // far en knapp for a gi den likevel.
            TempData["Feil"] = resultat.Melding;
            TempData["BekreftMedisinId"] = medisinId;
            return RedirectToAction(nameof(Index), new { dyrId });
        }

        if (!resultat.Ok)
        {
            return NotFound();
        }

        TempData["Melding"] = "Dosen er logget.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    [HttpPost("{medisinId:int}/avslutt")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Avslutt(
        int dyrId, int medisinId, CancellationToken ct)
    {
        if (!await _medisin.Avslutt(dyrId, medisinId, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Medisinen er avsluttet. Doseloggen er beholdt.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    private async Task<MedisinSideVm?> ByggSide(
        int dyrId, NyMedisinVm ny, CancellationToken ct)
    {
        var dyr = await _dyr.HentDetaljer(dyrId, ct);
        if (dyr is null)
        {
            return null;
        }

        return new MedisinSideVm
        {
            DyrId = dyrId,
            DyrNavn = dyr.Navn,
            Medisiner = await _medisin.HentFor(dyrId, ct),
            Ny = ny
        };
    }
}
