using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Eneste controller med offentlige handlinger. Alt annet er last av
/// fallback-policyen.
///
/// [AllowAnonymous] star per handling, ikke pa klassen. Pa klassen ville den
/// overstyrt [Authorize] pa LoggUt - klassenivaet vinner over handlingsnivaet.
/// </summary>
public sealed class KontoController : Controller
{
    private readonly SignInManager<Bruker> _paalogging;
    private readonly UserManager<Bruker> _brukere;
    private readonly IHusstandService _husstand;
    private readonly ILogger<KontoController> _log;

    public KontoController(
        SignInManager<Bruker> paalogging,
        UserManager<Bruker> brukere,
        IHusstandService husstand,
        ILogger<KontoController> log)
    {
        _paalogging = paalogging;
        _brukere = brukere;
        _husstand = husstand;
        _log = log;
    }

    [AllowAnonymous]
    [HttpGet("/logg-inn")]
    public IActionResult LoggInn()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Hjem");
        }

        return View(new LoggInnVm());
    }

    [AllowAnonymous]
    [HttpPost("/logg-inn")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoggInn(LoggInnVm vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        // isPersistent MA settes fra avkryssingsboksen. Uten den blir
        // informasjonskapselen en oktkapsel som dor nar nettleseren lukkes,
        // uansett hva ExpireTimeSpan sier. Plan kapittel 11.2.
        var resultat = await _paalogging.PasswordSignInAsync(
            vm.Epost,
            vm.Passord,
            isPersistent: vm.HuskMeg,
            lockoutOnFailure: true);

        if (resultat.Succeeded)
        {
            _log.LogInformation("Innlogging vellykket");
            return RedirectToAction("Index", "Hjem");
        }

        if (resultat.IsLockedOut)
        {
            ModelState.AddModelError(
                string.Empty,
                "Kontoen er midlertidig sperret etter for mange forsøk. "
                + "Prøv igjen om et kvarter.");
            return View(vm);
        }

        // Noytral melding. Skiller ikke mellom ukjent adresse og feil passord -
        // det ville bekreftet for en fremmed at adressen finnes i systemet.
        ModelState.AddModelError(
            string.Empty, "Feil e-postadresse eller passord.");
        return View(vm);
    }

    [AllowAnonymous]
    [HttpGet("/registrer")]
    public IActionResult Registrer() => View(new RegistrerVm());

    [AllowAnonymous]
    [HttpPost("/registrer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrer(RegistrerVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var epost = vm.Epost.Trim();

        // Identity bruker UserName som innloggingsnavn, ikke Email. Settes de
        // ikke like, feiler innlogging med e-post selv om passordet er riktig.
        var bruker = new Bruker
        {
            UserName = epost,
            Email = epost,
            Visningsnavn = vm.Visningsnavn.Trim()
        };

        var resultat = await _brukere.CreateAsync(bruker, vm.Passord);

        if (!resultat.Succeeded)
        {
            foreach (var feil in resultat.Errors)
            {
                ModelState.AddModelError(string.Empty, Oversett(feil));
            }

            return View(vm);
        }

        _log.LogInformation("Ny bruker registrert: {BrukerId}", bruker.Id);

        // Er adressen forhandsgodkjent av en husstand, knyttes brukeren til
        // den her - uten a taste noen kode. Da hopper de over oppsettsiden og
        // lander rett pa dashbordet. Se plan kapittel 12.3.
        var lagtTil = await _husstand.LosInnInvitasjon(bruker.Id, epost, ct);

        await _paalogging.SignInAsync(bruker, isPersistent: true);

        return lagtTil
            ? RedirectToAction("Index", "Hjem")
            : RedirectToAction("Oppsett", "Husstand");
    }

    [HttpPost("/logg-ut")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoggUt()
    {
        await _paalogging.SignOutAsync();
        _log.LogInformation("Utlogging");
        return RedirectToAction(nameof(LoggInn));
    }

    /// <summary>
    /// Identity svarer pa engelsk. Grensesnittet er norsk bokmal.
    /// Duplikat e-post far en noytral melding - se plan kapittel 15.
    /// </summary>
    private static string Oversett(IdentityError feil) => feil.Code switch
    {
        "DuplicateUserName" or "DuplicateEmail" =>
            "Denne adressen kan ikke brukes til registrering.",
        "PasswordTooShort" =>
            "Passordet må være minst 10 tegn.",
        "PasswordRequiresDigit" =>
            "Passordet må inneholde minst ett tall.",
        "PasswordRequiresLower" =>
            "Passordet må inneholde minst en liten bokstav.",
        "PasswordRequiresUpper" =>
            "Passordet må inneholde minst en stor bokstav.",
        "PasswordRequiresNonAlphanumeric" =>
            "Passordet må inneholde minst ett spesialtegn.",
        "InvalidEmail" =>
            "E-postadressen er ikke gyldig.",
        _ => "Registreringen kunne ikke fullføres."
    };
}
