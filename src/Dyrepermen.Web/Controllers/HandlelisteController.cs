using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Web.Extensions;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Handlingene svarer med et delvis view nar htmx spor, og omdirigerer
/// ellers. Da virker listen ogsa uten JavaScript - htmx gjor den raskere,
/// den er ikke en forutsetning for at den fungerer.
/// </summary>
[Route("handleliste")]
public sealed class HandlelisteController : Controller
{
    private readonly IHandlelisteService _liste;
    private readonly IDyrService _dyr;

    public HandlelisteController(IHandlelisteService liste, IDyrService dyr)
    {
        _liste = liste;
        _dyr = dyr;
    }

    private bool ErHtmx => Request.Headers.ContainsKey("HX-Request");

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await Bygg(ct));

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Legg(
        string tekst, int antall, int? dyrId, CancellationToken ct)
    {
        await _liste.Legg(
            new NyttPunkt(tekst ?? "", antall, dyrId, User.BrukerId()), ct);

        return await Svar(ct);
    }

    [HttpPost("{punktId:int}/kjopt")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkerKjopt(int punktId, CancellationToken ct)
    {
        if (!await _liste.VekslStatus(punktId, ct))
        {
            return NotFound();
        }

        return await Svar(ct);
    }

    [HttpPost("{punktId:int}/slett")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Slett(int punktId, CancellationToken ct)
    {
        if (!await _liste.Slett(punktId, ct))
        {
            return NotFound();
        }

        return await Svar(ct);
    }

    [HttpPost("rydd")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RyddKjopte(CancellationToken ct)
    {
        await _liste.RyddKjopte(ct);
        return await Svar(ct);
    }

    private async Task<IActionResult> Svar(CancellationToken ct)
    {
        var vm = await Bygg(ct);

        return ErHtmx
            ? PartialView("_Liste", vm)
            : RedirectToAction(nameof(Index));
    }

    private async Task<HandlelisteVm> Bygg(CancellationToken ct)
        => new()
        {
            Punkter = await _liste.Hent(ct),
            Dyr = (await _dyr.HentAlle(ct))
                .Select(d => new SelectListItem(d.Navn, d.Id.ToString()))
                .ToList()
        };
}
