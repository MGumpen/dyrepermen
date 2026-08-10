using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Tynn. All logikk ligger i IDyrService - controlleren mapper mellom
/// ViewModel og tjeneste, og oversetter resultattyper til meldinger.
/// </summary>
[Route("dyr")]
public sealed class DyrController : Controller
{
    private readonly IDyrService _dyr;

    public DyrController(IDyrService dyr) => _dyr = dyr;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _dyr.HentAlle(ct));

    [HttpGet("{dyrId:int}")]
    public async Task<IActionResult> Detaljer(int dyrId, CancellationToken ct)
    {
        var dyr = await _dyr.HentDetaljer(dyrId, ct);
        if (dyr is null)
        {
            return NotFound();
        }

        var sammendrag = await _dyr.HentSammendrag(dyrId, ct);
        if (sammendrag is null)
        {
            return NotFound();
        }

        return View(new DyrDetaljerVm { Dyr = dyr, Sammendrag = sammendrag });
    }

    [HttpGet("ny")]
    public IActionResult Ny() => View(new NyttDyrVm());

    [HttpPost("ny")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ny(NyttDyrVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var resultat = await _dyr.Opprett(
            new NyttDyr(vm.Navn, vm.Art, vm.Kjonn, vm.Rase, vm.Fodselsdato,
                vm.ChipNr, vm.RegNrNkk, vm.Kastrert), ct);

        if (!resultat.Ok)
        {
            ModelState.AddModelError(string.Empty, Melding(resultat.Feil));
            return View(vm);
        }

        return RedirectToAction(nameof(Detaljer), new { dyrId = resultat.DyrId });
    }

    [HttpGet("{dyrId:int}/rediger")]
    public async Task<IActionResult> Rediger(int dyrId, CancellationToken ct)
    {
        var d = await _dyr.HentDetaljer(dyrId, ct);
        if (d is null)
        {
            return NotFound();
        }

        return View(new RedigerDyrVm
        {
            Id = d.Id,
            Navn = d.Navn,
            Art = d.Art,
            Kjonn = d.Kjonn,
            Rase = d.Rase,
            Fodselsdato = d.Fodselsdato,
            ChipNr = d.ChipNr,
            RegNrNkk = d.RegNrNkk,
            Kastrert = d.Kastrert,
            ForingsloggAktiv = d.ForingsloggAktiv,
            ForplanAktiv = d.ForplanAktiv
        });
    }

    [HttpPost("{dyrId:int}/rediger")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rediger(
        int dyrId, RedigerDyrVm vm, CancellationToken ct)
    {
        // Ruteverdien er fasit, ikke det skjulte feltet i skjemaet.
        vm.Id = dyrId;

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var resultat = await _dyr.Oppdater(
            new RedigerDyr(dyrId, vm.Navn, vm.Art, vm.Kjonn, vm.Rase,
                vm.Fodselsdato, vm.ChipNr, vm.RegNrNkk, vm.Kastrert,
                vm.ForingsloggAktiv, vm.ForplanAktiv), ct);

        if (resultat.Feil is DyrFeil.FinnesIkke)
        {
            return NotFound();
        }

        if (!resultat.Ok)
        {
            ModelState.AddModelError(string.Empty, Melding(resultat.Feil));
            return View(vm);
        }

        TempData["Melding"] = "Endringene er lagret.";
        return RedirectToAction(nameof(Detaljer), new { dyrId });
    }

    [HttpPost("{dyrId:int}/deaktiver")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deaktiver(int dyrId, CancellationToken ct)
    {
        if (!await _dyr.Deaktiver(dyrId, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Dyret er deaktivert. Historikken er beholdt.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Meldingene er noytrale med vilje. Unikheten pa chipnummer er global,
    /// sa kollisjonen kan komme fra et dyr i en annen husstand - eller fra et
    /// deaktivert dyr query-filteret skjuler. Teksten skal ikke avslore
    /// hverken hvilken husstand eller at raden finnes. Se plan kapittel 5.3.
    /// </summary>
    private static string Melding(DyrFeil feil) => feil switch
    {
        DyrFeil.ChipFinnes => "Chipnummeret er allerede registrert på et dyr.",
        DyrFeil.RegnrFinnes => "Registreringsnummeret er allerede i bruk.",
        DyrFeil.Samtidighet =>
            "Noen andre endret dette mens du redigerte — last siden på nytt.",
        _ => "Verdien finnes allerede."
    };
}
