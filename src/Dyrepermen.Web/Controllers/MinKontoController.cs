using System.Text;
using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Web.Extensions;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Innlogget kontoadministrasjon. Skilt fra KontoController, som har de
/// anonyme handlingene - a blande dem i en klasse var nettopp det som gjorde
/// at [AllowAnonymous] overstyrte [Authorize] pa utlogging tidligere.
/// </summary>
[Route("konto")]
public sealed class MinKontoController : Controller
{
    private readonly IKontoService _konto;
    private readonly SignInManager<Bruker> _paalogging;

    public MinKontoController(
        IKontoService konto, SignInManager<Bruker> paalogging)
    {
        _konto = konto;
        _paalogging = paalogging;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = await Bygg(ct);
        return vm is null ? Forbid() : View(vm);
    }

    [HttpGet("data")]
    public async Task<IActionResult> LastNedData(CancellationToken ct)
    {
        var brukerId = User.BrukerId();
        if (brukerId is null)
        {
            return Forbid();
        }

        var json = await _konto.EksporterJson(brukerId.Value, ct);

        return File(
            Encoding.UTF8.GetBytes(json),
            "application/json",
            $"dyrepermen-{DateTime.Now:yyyy-MM-dd}.json");
    }

    [HttpPost("slett")]
    [ValidateAntiForgeryToken]
    // Parameteren MA hete "slett": skjemaet poster "Slett.Passord", og
    // modellbinderen bruker parameternavnet som prefiks. Heter den noe annet,
    // binder ingenting - og siden lastes pa nytt uten a gjore noe.
    public async Task<IActionResult> Slett(SlettKontoVm slett, CancellationToken ct)
    {
        var brukerId = User.BrukerId();
        if (brukerId is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var side = await Bygg(ct);
            return side is null ? Forbid() : View(nameof(Index), side);
        }

        var resultat = await _konto.SlettBruker(
            brukerId.Value, slett.Passord, slett.BekrefterHusstandsletting, ct);

        if (resultat is SlettResultat.Ok)
        {
            // Informasjonskapselen er gyldig i 30 dager til og ma
            // ugyldiggjores umiddelbart. Se plan kapittel 12.5.
            await _paalogging.SignOutAsync();
            return RedirectToAction("LoggInn", "Konto");
        }

        ModelState.AddModelError(string.Empty, resultat switch
        {
            SlettResultat.FeilPassord => "Feil passord.",
            SlettResultat.MaBekrefteHusstandsletting =>
                "Du er eneste medlem. Kryss av for at husstanden og alle "
                + "data slettes.",
            _ => "Kontoen kunne ikke slettes."
        });

        var paaNytt = await Bygg(ct);
        if (paaNytt is null)
        {
            return Forbid();
        }

        paaNytt.Slett.Passord = string.Empty;
        return View(nameof(Index), paaNytt);
    }

    private async Task<MinKontoVm?> Bygg(CancellationToken ct)
    {
        var brukerId = User.BrukerId();
        if (brukerId is null)
        {
            return null;
        }

        var (sisteMedlem, antallDyr) =
            await _konto.Slettekonsekvens(brukerId.Value, ct);

        return new MinKontoVm
        {
            Visningsnavn = User.Identity?.Name ?? "",
            Slett = new SlettKontoVm
            {
                ErSisteMedlem = sisteMedlem,
                AntallDyr = antallDyr
            }
        };
    }
}
