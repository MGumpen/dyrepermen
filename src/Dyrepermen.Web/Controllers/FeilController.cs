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
    [HttpGet("/feil")]
    [HttpGet("/feil/{kode:int}")]
    public IActionResult Index(int? kode)
    {
        ViewData["Kode"] = kode;
        return View();
    }
}
