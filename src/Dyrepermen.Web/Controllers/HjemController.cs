using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Dashbord etter innlogging. Ingen offentlig forside - rot-URL-en sender
/// uautentiserte brukere til /logg-inn via fallback-policyen.
///
/// Handlingene her finnes ogsa andre steder i appen. De ligger likevel pa
/// dashbordet fordi det daglige - gi mat, kryss av en vare - skal kunne
/// gjores uten a navigere. De er tynne: de gjor ingenting selv, men kaller
/// samme tjeneste som de ordinaere sidene og returnerer dashbordets egen
/// utgave av listen.
/// </summary>
public sealed class HjemController : Controller
{
    private readonly IDashbordService _dashbord;
    private readonly IForingService _foring;
    private readonly IForplanService _forplan;
    private readonly IHandlelisteService _handleliste;
    private readonly IGjeldendeBruker _meg;

    public HjemController(
        IDashbordService dashbord,
        IForingService foring,
        IForplanService forplan,
        IHandlelisteService handleliste,
        IGjeldendeBruker meg)
    {
        _dashbord = dashbord;
        _foring = foring;
        _forplan = forplan;
        _handleliste = handleliste;
        _meg = meg;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _dashbord.Hent(ct));

    /// <summary>
    /// Registrerer ett maltid med mengden planen sier.
    ///
    /// Gjesteapent med vilje: passer du hunden, er det nettopp dette du skal
    /// kunne gjore. Se ADR 0009.
    /// </summary>
    [HttpPost("/dashbord/mat/{dyrId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GiMat(int dyrId, CancellationToken ct)
    {
        var plan = await _forplan.BeregnAktiv(dyrId, ct);

        // Mengden regnes HER, pa serveren. Sendte knappen tallet med seg,
        // kunne en faneside som har statt apen siden i gar skrevet en porsjon
        // som ikke lenger stemmer med planen - og for en valp endrer den seg
        // hver gang vekten registreres.
        int? porsjon = plan is { HarPlan: true, ManglerVekt: false }
            ? plan.PorsjonGram
            : null;

        // Returverdien ignoreres med vilje. False betyr at foringsloggen er
        // slatt av, eller at dyret horer til en annen husstand - og da har
        // knappen uansett ikke blitt tegnet i denne omgangen. Svaret under er
        // dashbordets faktiske tilstand, sa en foreldet fane retter seg selv.
        await _foring.Registrer(
            new NyForing(dyrId, porsjon, null, _meg.BrukerId), ct);

        if (!ErHtmx)
        {
            return RedirectToAction(nameof(Index));
        }

        return PartialView("_Dyreliste", (await _dashbord.Hent(ct)).Dyr);
    }

    /// <summary>Veksler mellom kjopt og aktiv, som pa handlelistesiden.</summary>
    [HttpPost("/dashbord/handleliste/{punktId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> KryssAv(int punktId, CancellationToken ct)
    {
        await _handleliste.VekslStatus(punktId, ct);

        if (!ErHtmx)
        {
            return RedirectToAction(nameof(Index));
        }

        // Punktet forsvinner fra dashbordet nar det krysses av - listen viser
        // kun aktive. Angring skjer pa handlelistesiden, der bade kjopte og
        // aktive star.
        return PartialView(
            "_Handleliste",
            await _handleliste.HentAktive(DashbordService.AntallPaHandleliste, ct));
    }

    /// <summary>
    /// Uten htmx ender skjemaene i en vanlig innsending, og da skal svaret
    /// vaere hele dashbordet - ikke et lost listefragment uten hode og meny.
    /// Skjemaene har derfor bade asp-action og hx-post.
    /// </summary>
    private bool ErHtmx => Request.Headers.ContainsKey("HX-Request");
}
