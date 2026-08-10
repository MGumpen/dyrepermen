using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Dashbord etter innlogging. Ingen offentlig forside - rot-URL-en sender
/// uautentiserte brukere til /logg-inn via fallback-policyen.
///
/// Innholdet (dyrekort, forfaller snart, handleliste) bygges i fase 1b.
/// </summary>
public sealed class HjemController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View();
}
