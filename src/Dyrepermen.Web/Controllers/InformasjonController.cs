using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Web.Extensions;
using Dyrepermen.Web.Filtre;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Samleside: alt vi vet om hvert dyr, pluss frie notater. Siden du kan
/// vise fram til dyrepasseren.
/// </summary>
[Route("informasjon")]
public sealed class InformasjonController : Controller
{
    private readonly IInformasjonService _info;
    private readonly IDyrService _dyr;

    public InformasjonController(IInformasjonService info, IDyrService dyr)
    {
        _info = info;
        _dyr = dyr;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await Bygg(new NyttNotatVm(), ct));

    [HttpGet("{id:int}/rediger")]
    public async Task<IActionResult> Rediger(int id, CancellationToken ct)
    {
        var rad = await _info.HentEn(id, ct);
        if (rad is null)
        {
            return NotFound();
        }

        return View(nameof(Index), await Bygg(new NyttNotatVm
        {
            Id = rad.Id,
            Tittel = rad.Tittel,
            Tekst = rad.Tekst,
            DyrId = rad.DyrId
        }, ct));
    }

    [HttpPost("")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lagre(NyttNotatVm ny, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), await Bygg(ny, ct));
        }

        var ok = await _info.Lagre(new NyInformasjon(
            ny.Id, ny.Tittel, ny.Tekst, ny.DyrId, User.BrukerId()), ct);

        if (!ok)
        {
            return NotFound();
        }

        TempData["Melding"] = ny.Id is null
            ? "Notatet er lagret."
            : "Notatet er oppdatert.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/slett")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Slett(int id, CancellationToken ct)
    {
        if (!await _info.Slett(id, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Notatet er slettet.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<InformasjonVm> Bygg(NyttNotatVm ny, CancellationToken ct)
    {
        var alle = await _info.Hent(ct);

        return new InformasjonVm
        {
            Dyr = await _info.HentDyreoversikt(ct),
            FellesNotater = alle.Where(n => n.DyrId is null).ToList(),
            DyrValg = (await _dyr.HentAlle(ct))
                .Select(d => new SelectListItem(d.Navn, d.Id.ToString()))
                .ToList(),
            Ny = ny
        };
    }
}
