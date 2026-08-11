using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Web.Filtre;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Eget punkt i menyen, ikke under hvert dyr. En husstand har gjerne like
/// mange poliser som dyr, og de sammenlignes med hverandre - premie mot
/// dekning mot egenandel. Da hoerer de hjemme pa samme side.
/// </summary>
[Route("forsikring")]
public sealed class ForsikringController : Controller
{
    private readonly IForsikringService _forsikring;
    private readonly IDyrService _dyr;

    public ForsikringController(IForsikringService forsikring, IDyrService dyr)
    {
        _forsikring = forsikring;
        _dyr = dyr;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await Bygg(new NyForsikringVm(), ct));

    [HttpGet("{id:int}/rediger")]
    public async Task<IActionResult> Rediger(int id, CancellationToken ct)
    {
        var rad = await _forsikring.HentEn(id, ct);
        if (rad is null)
        {
            return NotFound();
        }

        return View(nameof(Index), await Bygg(new NyForsikringVm
        {
            Id = rad.Id,
            DyrId = rad.DyrId,
            Selskap = rad.Selskap,
            PoliseNr = rad.PoliseNr,
            ArspremieKr = rad.ArspremieKr,
            ForsikringsbelopKr = rad.ForsikringsbelopKr,
            EgenandelFastKr = rad.EgenandelFastKr,
            EgenandelVariabelProsent = rad.EgenandelVariabelTidels / 10m,
            FornyesDato = rad.FornyesDato
        }, ct));
    }

    [HttpPost("")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lagre(NyForsikringVm ny, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), await Bygg(ny, ct));
        }

        var ok = await _forsikring.Lagre(new NyForsikring(
            ny.Id,
            ny.DyrId,
            ny.Selskap,
            ny.PoliseNr,
            ny.ArspremieKr,
            ny.ForsikringsbelopKr,
            ny.EgenandelFastKr,
            // 20 % skrives som 20 og lagres som 200 tidels.
            (int)Math.Round(ny.EgenandelVariabelProsent * 10),
            ny.FornyesDato), ct);

        if (!ok)
        {
            return NotFound();
        }

        TempData["Melding"] = ny.Id is null
            ? "Forsikringen er lagret."
            : "Forsikringen er oppdatert.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/slett")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Slett(int id, CancellationToken ct)
    {
        if (!await _forsikring.Slett(id, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Forsikringen er slettet.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ForsikringVm> Bygg(NyForsikringVm ny, CancellationToken ct)
        => new()
        {
            Poliser = await _forsikring.Hent(ct),
            DyrValg = (await _dyr.HentAlle(ct))
                .Select(d => new SelectListItem(d.Navn, d.Id.ToString()))
                .ToList(),
            Ny = ny
        };
}
