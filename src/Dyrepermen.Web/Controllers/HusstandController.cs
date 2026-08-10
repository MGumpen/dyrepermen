using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Web.Extensions;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

[Route("husstand")]
public sealed class HusstandController : Controller
{
    private readonly IHusstandService _husstand;
    private readonly SignInManager<Bruker> _paalogging;
    private readonly UserManager<Bruker> _brukere;

    public HusstandController(
        IHusstandService husstand,
        SignInManager<Bruker> paalogging,
        UserManager<Bruker> brukere)
    {
        _husstand = husstand;
        _paalogging = paalogging;
        _brukere = brukere;
    }

    [HttpGet("oppsett")]
    public IActionResult Oppsett() => View(new OppsettVm());

    [HttpPost("opprett")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Opprett(OppsettVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Oppsett), vm);
        }

        var brukerId = User.BrukerId();
        if (brukerId is null)
        {
            return Forbid();
        }

        await _husstand.OpprettHusstand(vm.Navn.Trim(), brukerId.Value, ct);

        // Ikke pakrevd for at query-filtrene skal virke - husstand leses fra
        // database (ADR 0001). Beholdt fordi det koster ingenting og gjor
        // oppforselen forutsigbar hvis noen senere legger claim-en tilbake.
        var bruker = await _brukere.GetUserAsync(User);
        if (bruker is not null)
        {
            await _paalogging.RefreshSignInAsync(bruker);
        }

        return RedirectToAction("Index", "Hjem");
    }
}
