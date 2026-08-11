using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Web.Extensions;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Dyrepermen.Web.Middleware;
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

    /// <summary>
    /// Bytter aktiv husstand. Verdien er kun et VALG - middlewaren validerer
    /// den mot medlemskapene ved hver foresporsel, sa en manipulert kapsel
    /// gir ingen tilgang. Se ADR 0009.
    /// </summary>
    [HttpPost("bytt")]
    [ValidateAntiForgeryToken]
    public IActionResult Bytt(int husstandId, string? retur)
    {
        Response.Cookies.Append(
            HusstandMiddleware.Kapsel,
            husstandId.ToString(),
            new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });

        // Kun lokale stier. Uten sjekken kan lenken sende brukeren til et
        // fremmed nettsted etter innlogging - apen omdirigering.
        return Url.IsLocalUrl(retur) ? Redirect(retur!) : RedirectToAction("Index", "Hjem");
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
