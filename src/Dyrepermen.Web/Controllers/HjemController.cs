using Dyrepermen.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Dashbord etter innlogging. Ingen offentlig forside - rot-URL-en sender
/// uautentiserte brukere til /logg-inn via fallback-policyen.
/// </summary>
public sealed class HjemController : Controller
{
    private readonly IDashbordService _dashbord;

    public HjemController(IDashbordService dashbord) => _dashbord = dashbord;

    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _dashbord.Hent(ct));
}
