using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Web.Extensions;
using Dyrepermen.Web.Filtre;
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
    private readonly IDokumentService _dokumenter;

    public DyrController(IDyrService dyr, IDokumentService dokumenter)
    {
        _dyr = dyr;
        _dokumenter = dokumenter;
    }

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
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ny(NyttDyrVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var resultat = await _dyr.Opprett(
            new NyttDyr(vm.Navn, vm.Art, vm.Kjonn, vm.Rase, vm.Fodselsdato,
                vm.ChipNr, vm.RegNrNkk, vm.Kastrert, vm.Farge, vm.Kjennetegn), ct);

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
            Farge = d.Farge,
            Kjennetegn = d.Kjennetegn,
            ProfilbildeId = d.ProfilbildeId,
            ForingsloggAktiv = d.ForingsloggAktiv,
            ForplanAktiv = d.ForplanAktiv
        });
    }

    [HttpPost("{dyrId:int}/rediger")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rediger(
        int dyrId, RedigerDyrVm vm, CancellationToken ct)
    {
        // Ruteverdien er fasit, ikke det skjulte feltet i skjemaet.
        vm.Id = dyrId;

        if (!ModelState.IsValid)
        {
            return await RedigerPaNytt(vm, ct);
        }

        var resultat = await _dyr.Oppdater(
            new RedigerDyr(dyrId, vm.Navn, vm.Art, vm.Kjonn, vm.Rase,
                vm.Fodselsdato, vm.ChipNr, vm.RegNrNkk, vm.Kastrert,
                vm.Farge, vm.Kjennetegn,
                vm.ForingsloggAktiv, vm.ForplanAktiv), ct);

        if (resultat.Feil is DyrFeil.FinnesIkke)
        {
            return NotFound();
        }

        if (!resultat.Ok)
        {
            ModelState.AddModelError(string.Empty, Melding(resultat.Feil));
            return await RedigerPaNytt(vm, ct);
        }

        TempData["Melding"] = "Endringene er lagret.";
        return RedirectToAction(nameof(Detaljer), new { dyrId });
    }

    /// <summary>
    /// Laster opp profilbildet, og bytter ut det gamle. Egen handling, ikke en
    /// del av redigeringsskjemaet: en valideringsfeil i navnet skal ikke koste
    /// bildet brukeren nettopp valgte, og omvendt. Se ADR 0018.
    /// </summary>
    [HttpPost("{dyrId:int}/profilbilde")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(Vedleggsregler.MaksForesporselByte)]
    [RequestFormLimits(MultipartBodyLengthLimit = Vedleggsregler.MaksForesporselByte)]
    public async Task<IActionResult> LagreProfilbilde(
        int dyrId, IFormFile? bilde, CancellationToken ct)
    {
        if (bilde is null)
        {
            TempData["Feil"] = "Velg et bilde.";
            return TilBildet(dyrId);
        }

        var resultat = await _dokumenter.LagreProfilbilde(
            dyrId, await bilde.TilVedlegg(ct), ct);

        if (resultat.FinnesIkke)
        {
            return NotFound();
        }

        if (resultat.Ok)
        {
            TempData["Melding"] = "Bildet er lagret.";
        }
        else
        {
            TempData["Feil"] = resultat.Feil;
        }

        return TilBildet(dyrId);
    }

    [HttpPost("{dyrId:int}/profilbilde/fjern")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FjernProfilbilde(int dyrId, CancellationToken ct)
    {
        if (!await _dokumenter.FjernProfilbilde(dyrId, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Bildet er fjernet.";
        return TilBildet(dyrId);
    }

    private RedirectToActionResult TilBildet(int dyrId)
        => RedirectToAction(nameof(Rediger), null, new { dyrId }, "profilbilde");

    /// <summary>
    /// Skjemaet tegnes pa nytt med feilmeldingene. Bildet er ikke en del av
    /// det som postes, og hentes inn igjen for forhandsvisningen.
    /// </summary>
    private async Task<IActionResult> RedigerPaNytt(RedigerDyrVm vm, CancellationToken ct)
    {
        vm.ProfilbildeId = (await _dyr.HentDetaljer(vm.Id, ct))?.ProfilbildeId;
        return View(vm);
    }

    [HttpPost("{dyrId:int}/deaktiver")]
    [KreverEier]
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
