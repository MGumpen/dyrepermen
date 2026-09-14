using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Web.Extensions;
using Dyrepermen.Web.Filtre;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Registrering skal vaere sa naer ett klikk som mulig, og er apen for
/// gjester: passer du hunden, ma du kunne notere at du ga mat. Redigering og
/// sletting krever beboer.
/// </summary>
[Route("dyr/{dyrId:int}/foring")]
public sealed class ForingController : Controller
{
    private readonly IForingService _foring;
    private readonly IForplanService _forplan;
    private readonly IDyrService _dyr;

    public ForingController(
        IForingService foring, IForplanService forplan, IDyrService dyr)
    {
        _foring = foring;
        _forplan = forplan;
        _dyr = dyr;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int dyrId, CancellationToken ct)
    {
        var vm = await Bygg(dyrId, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrer(
        int dyrId, int? mengdeGram, string? kommentar, CancellationToken ct)
    {
        // Tidspunkt er bevisst ikke en parameter. Tjenesten setter det selv.
        var ok = await _foring.Registrer(
            new NyForing(dyrId, mengdeGram, kommentar, User.BrukerId()), ct);

        if (!ok)
        {
            return NotFound();
        }

        TempData["Melding"] = "Fôringen er registrert.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    [HttpPost("{foringId:int}/tid")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RedigerTid(
        int dyrId, int foringId, DateTime tidspunkt, CancellationToken ct)
    {
        // Skjemaet sender lokal tid uten sone. Den tolkes som norsk og
        // konverteres til UTC for lagring.
        var lokal = new DateTimeOffset(tidspunkt, Tidssone.Forskyvning(tidspunkt));

        if (!await _foring.RedigerTid(dyrId, foringId, lokal, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Tidspunktet er rettet.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    [HttpPost("{foringId:int}/slett")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Slett(
        int dyrId, int foringId, CancellationToken ct)
    {
        if (!await _foring.Slett(dyrId, foringId, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Fôringen er slettet.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    private async Task<ForingSideVm?> Bygg(int dyrId, CancellationToken ct)
    {
        var dyr = await _dyr.HentDetaljer(dyrId, ct);

        // Bryteren av: siden finnes ikke. Samme svar som for et dyr i en
        // annen husstand.
        if (dyr is null || !dyr.ForingsloggAktiv)
        {
            return null;
        }

        var vm = new ForingSideVm
        {
            DyrId = dyrId,
            DyrNavn = dyr.Navn,
            Historikk = await _foring.HentFor(dyrId, ct)
        };

        // Forhandsutfyll mengde fra forplanen. Mangler vektgrunnlag, star
        // feltet tomt - ikke 0.
        if (dyr.ForplanAktiv)
        {
            var plan = await _forplan.BeregnAktiv(dyrId, ct);
            if (plan is { HarPlan: true, ManglerVekt: false })
            {
                var fordeling = Maltidsfordeling.Fordel(
                    plan.GramPerDag, plan.AntallMaltider);

                vm.ForeslattMengde = fordeling.Length > 0 ? fordeling[0] : null;
                vm.AntallMaltider = plan.AntallMaltider;
            }
        }

        return vm;
    }
}
