using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Noytral feilside uten stakksporing eller melding fra databasen. En
/// PostgresException som lekker til brukeren kan avslore tabellnavn og
/// constraint-navn. Se plan kapittel 9.2.
/// </summary>
[AllowAnonymous]
public sealed class FeilController : Controller
{
    /// <summary>
    /// Vises nar en gjest prover a endre noe. Cookie-autentisering svarer
    /// pa Forbid() med en omdirigering hit, ikke med 403 - og uten en egen
    /// side ville brukeren havnet pa innloggingssiden og trodd hun var
    /// logget ut. Se ADR 0009.
    /// </summary>
    [HttpGet("/ingen-tilgang")]
    public IActionResult IngenTilgang() => View();

    [HttpGet("/feil")]
    [HttpGet("/feil/{kode:int}")]
    public IActionResult Index(int? kode)
    {
        ViewData["Kode"] = kode;
        return View();
    }
}
