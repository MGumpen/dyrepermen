using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Web.Extensions;
using Dyrepermen.Web.Filtre;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

[Route("dyr/{dyrId:int}/medisin")]
public sealed class MedisinController : Controller
{
    private readonly IMedisinService _medisin;
    private readonly IDyrService _dyr;

    public MedisinController(IMedisinService medisin, IDyrService dyr)
    {
        _medisin = medisin;
        _dyr = dyr;
    }

    /// <summary>
    /// <paramref name="rediger"/> setter skjemaet i endringsmodus for den ene
    /// medisinen - for eksempel nar dosen trappes ned. Samme monster som
    /// behandlingssiden: listen star synlig ved siden av mens man retter.
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(
        int dyrId, int? rediger, CancellationToken ct)
    {
        var vm = await ByggSide(dyrId, ny: null, redigerId: rediger, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost("")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ny(
        int dyrId, NyMedisinVm ny, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var vm = await ByggSide(dyrId, ny, redigerId: null, ct);
            return vm is null ? NotFound() : View(nameof(Index), vm);
        }

        var ok = await _medisin.Registrer(Innhold(dyrId, ny), ct);

        if (!ok)
        {
            return NotFound();
        }

        TempData["Melding"] = "Medisinen er lagt til.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    /// <summary>
    /// <paramref name="fraOversikt"/> sender brukeren tilbake til dashbordet
    /// etter dosen, der knappen sto i "Forfaller snart". Et flagg og ikke en
    /// fri returadresse: det finnes bare to steder a lande, og en fri adresse
    /// ville matte valideres mot apen omdirigering.
    ///
    /// Advarselen om for tidlig dose lander alltid pa medisinsiden. Det er
    /// der knappen for a gi den likevel finnes.
    /// </summary>
    [HttpPost("{medisinId:int}/dose")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoggDose(
        int dyrId, int medisinId, bool bekreft, bool fraOversikt,
        CancellationToken ct)
    {
        var resultat = await _medisin.LoggDose(
            dyrId, medisinId, User.BrukerId(), bekreft, ct);

        if (resultat.KreverBekreftelse)
        {
            // Ikke en feil - en advarsel. Dosen er ikke logget, og brukeren
            // far en knapp for a gi den likevel.
            TempData["Feil"] = resultat.Melding;
            TempData["BekreftMedisinId"] = medisinId;
            return RedirectToAction(nameof(Index), new { dyrId });
        }

        if (!resultat.Ok)
        {
            return NotFound();
        }

        TempData["Melding"] = "Dosen er logget.";

        return fraOversikt
            ? RedirectToAction(nameof(HjemController.Index), "Hjem")
            : RedirectToAction(nameof(Index), new { dyrId });
    }

    [HttpPost("{medisinId:int}/rediger")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rediger(
        int dyrId, int medisinId, NyMedisinVm ny, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            // redigerId beholdes, sa skjemaet star igjen i endringsmodus med
            // feilmeldingene - ikke som et tomt registreringsskjema.
            var vm = await ByggSide(dyrId, ny, redigerId: medisinId, ct);
            return vm is null ? NotFound() : View(nameof(Index), vm);
        }

        if (!await _medisin.Oppdater(dyrId, medisinId, Innhold(dyrId, ny), ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Medisinen er oppdatert.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    /// <summary>Tomt intervall betyr 0 - ingen fast gjentakelse.</summary>
    private static NyMedisin Innhold(int dyrId, NyMedisinVm ny)
        => new(dyrId, ny.Navn, ny.Dose, ny.IntervallTimer ?? 0,
            ny.StartDato, ny.SluttDato);

    [HttpPost("{medisinId:int}/avslutt")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Avslutt(
        int dyrId, int medisinId, CancellationToken ct)
    {
        if (!await _medisin.Avslutt(dyrId, medisinId, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Medisinen er avsluttet. Doseloggen er beholdt.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    /// <summary>
    /// <paramref name="ny"/> er null nar skjemaet ikke er sendt inn. Da
    /// fylles det fra medisinen som skal endres, eller star tomt.
    /// </summary>
    private async Task<MedisinSideVm?> ByggSide(
        int dyrId, NyMedisinVm? ny, int? redigerId, CancellationToken ct)
    {
        var dyr = await _dyr.HentDetaljer(dyrId, ct);
        if (dyr is null)
        {
            return null;
        }

        var medisiner = await _medisin.HentFor(dyrId, ct);

        // Medisinen kan vaere slettet sammen med dyret i en annen fane. Da
        // faller siden tilbake til et tomt skjema, ikke en feilside.
        var rad = redigerId is { } id
            ? medisiner.FirstOrDefault(m => m.Id == id)
            : null;

        return new MedisinSideVm
        {
            DyrId = dyrId,
            DyrNavn = dyr.Navn,
            Medisiner = medisiner,
            RedigerId = rad?.Id,
            Ny = ny ?? (rad is null ? new NyMedisinVm() : new NyMedisinVm
            {
                Navn = rad.Navn,
                Dose = rad.Dose,
                // 0 vises som tomt felt, slik som i et nytt skjema.
                IntervallTimer = rad.IntervallTimer > 0 ? rad.IntervallTimer : null,
                StartDato = rad.StartDato,
                SluttDato = rad.SluttDato
            })
        };
    }
}
