using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Sporsmal og onsker om endringer, sendt pa e-post til den som lager appen.
/// Ligger over brukerkortet i menyen, ikke blant husstandens sider: den
/// gjelder brukeren og appen, ikke husstanden. Se ADR 0014.
/// </summary>
[Route("kontakt")]
public sealed class KontaktController : Controller
{
    /// <summary>Navnet pa grensen for innsendinger, registrert i Program.cs.</summary>
    public const string Grense = "kontakt";

    private readonly IKontaktService _kontakt;

    public KontaktController(IKontaktService kontakt) => _kontakt = kontakt;

    [HttpGet("")]
    public IActionResult Index() => View(new KontaktVm());

    // Ingen [KreverEier]: en gjest skal kunne si fra like godt som den som
    // bor der. Star derfor pa gjestelisten i RolleTester.
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(Grense)]
    public async Task<IActionResult> Send(KontaktVm kontakt, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), kontakt);
        }

        var resultat = await _kontakt.Send(
            kontakt.Type!.Value, kontakt.Melding.Trim(), ct);

        if (resultat == Kontaktresultat.Sendt)
        {
            TempData["Melding"] = "Takk! Meldingen er sendt.";
            return RedirectToAction(nameof(Index));
        }

        // Skjemaet vises pa nytt med teksten i behold. Den som har skrevet et
        // langt avsnitt, skal ikke matte skrive det igjen.
        ModelState.AddModelError(string.Empty, resultat == Kontaktresultat.IkkeSattOpp
            ? "Kontaktskjemaet er ikke satt opp ennå. Prøv igjen senere."
            : "Meldingen kunne ikke sendes akkurat nå. Prøv igjen om litt.");

        return View(nameof(Index), kontakt);
    }
}
