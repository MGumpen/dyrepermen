using System.Globalization;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// «Prov en demo» fra innloggingssiden. Se ADR 0015.
///
/// Den eneste nye anonyme inngangen i appen. Alt bak den er den vanlige,
/// innloggede appen med de samme filtrene og rollene.
/// </summary>
[Route("demo")]
public sealed class DemoController : Controller
{
    /// <summary>Navnet pa grensen for antall demoer per IP, se Program.cs.</summary>
    public const string Grense = "demo";

    private readonly IDemoService _demo;
    private readonly UserManager<Bruker> _brukere;
    private readonly SignInManager<Bruker> _paalogging;

    public DemoController(
        IDemoService demo,
        UserManager<Bruker> brukere,
        SignInManager<Bruker> paalogging)
    {
        _demo = demo;
        _brukere = brukere;
        _paalogging = paalogging;
    }

    // POST og ikke en lenke: klikket oppretter data. En lenke ville latt
    // sokemotorer og forhandshenting i nettleseren lage demoer.
    [HttpPost("")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(Grense)]
    public async Task<IActionResult> Start(CancellationToken ct)
    {
        // Allerede innlogget: ingen ny demo oppa den gamle innloggingen.
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect("/");
        }

        var brukerId = await _demo.Start(ct);
        if (brukerId is null)
        {
            TempData["Feil"] = "Mange prøver demoen akkurat nå. Prøv igjen om litt.";
            return RedirectToAction("LoggInn", "Konto");
        }

        var bruker = await _brukere.FindByIdAsync(
            brukerId.Value.ToString(CultureInfo.InvariantCulture));

        // Ikke vedvarende: demoen skal ikke overleve at nettleseren lukkes,
        // slik 30 dagers «husk meg» ville gjort.
        await _paalogging.SignInAsync(bruker!, isPersistent: false);

        return Redirect("/");
    }
}
