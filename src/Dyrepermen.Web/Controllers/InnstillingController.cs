using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Web.Filtre;
using Dyrepermen.Web.Extensions;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

[Route("innstillinger")]
public sealed class InnstillingController : Controller
{
    private readonly IHusstandService _husstand;

    public InnstillingController(IHusstandService husstand) => _husstand = husstand;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = await Bygg(ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost("lagre")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lagre(
        LagreInnstillingerVm inn, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var side = await Bygg(ct);
            if (side is null)
            {
                return NotFound();
            }

            side.Husstandsnavn = inn.Husstandsnavn;
            return View(nameof(Index), side);
        }

        await _husstand.LagreInnstillinger(
            inn.Husstandsnavn, inn.ForingsloggStandard,
            inn.ForplanStandard, inn.VarslerAktiv, inn.GodbitloggAktiv, ct);

        TempData["Melding"] = "Innstillingene er lagret.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("medlem")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LeggTilMedlem(
        string? nyttMedlemEpost, Husstandsrolle rolle, CancellationToken ct)
    {
        var brukerId = User.BrukerId();
        if (brukerId is null)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(nyttMedlemEpost))
        {
            TempData["Feil"] = "Skriv inn en e-postadresse.";
            return RedirectToAction(nameof(Index));
        }

        var resultat = await _husstand.LeggTilMedlem(
            nyttMedlemEpost, rolle, brukerId.Value, ct);

        // Meldingen ved TilhorerAnnenHusstand er noytral med vilje. A svare
        // "personen tilhorer allerede en husstand" ville bekreftet for en
        // fremmed at adressen er registrert i systemet. Se plan kapittel 12.3.
        switch (resultat)
        {
            case LeggTilResultat.LagtTil:
                TempData["Melding"] = "Medlemmet er lagt til.";
                break;
            case LeggTilResultat.VenterPaRegistrering:
                TempData["Melding"] =
                    "Adressen er godkjent. Personen blir med automatisk når "
                    + "de registrerer seg.";
                break;
            default:
                TempData["Melding"] = "Denne personen er allerede medlem.";
                break;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("invitasjon/{invitasjonId:int}/angre")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AngreInvitasjon(
        int invitasjonId, CancellationToken ct)
    {
        if (await _husstand.AngreInvitasjon(invitasjonId, ct))
        {
            TempData["Melding"] = "Invitasjonen er trukket tilbake.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("medlem/{brukerId:int}/fjern")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FjernMedlem(
        int brukerId, CancellationToken ct)
    {
        if (await _husstand.FjernMedlem(brukerId, ct))
        {
            TempData["Melding"] = "Medlemmet er fjernet.";
        }
        else
        {
            // Applikasjonen tillater ikke en husstand uten medlemmer.
            TempData["Feil"] =
                "Kan ikke fjerne det siste medlemmet. Legg til noen først, "
                + "eller slett kontoen din.";
        }

        // Fjerner du deg selv, sender middlewaren deg til oppsettsiden ved
        // neste sidevisning.
        return RedirectToAction(nameof(Index));
    }

    private async Task<InnstillingerVm?> Bygg(CancellationToken ct)
    {
        var brukerId = User.BrukerId();
        if (brukerId is null)
        {
            return null;
        }

        var oversikt = await _husstand.HentOversikt(brukerId.Value, ct);
        if (oversikt is null)
        {
            return null;
        }

        return new InnstillingerVm
        {
            Oversikt = oversikt,
            Husstandsnavn = oversikt.Navn,
            ForingsloggStandard = oversikt.ForingsloggStandard,
            ForplanStandard = oversikt.ForplanStandard,
            VarslerAktiv = oversikt.VarslerAktiv,
            GodbitloggAktiv = oversikt.GodbitloggAktiv
        };
    }
}
