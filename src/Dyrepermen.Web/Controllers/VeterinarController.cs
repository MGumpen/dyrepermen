using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Web.Filtre;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Eget punkt i menyen. Stedene og timene horer sammen: du apner siden enten
/// fordi noe har skjedd og du trenger et nummer, eller fordi du skal se nar
/// neste time er.
/// </summary>
[Route("veterinar")]
public sealed class VeterinarController : Controller
{
    private readonly IVeterinarService _veterinar;
    private readonly IDyrService _dyr;

    public VeterinarController(IVeterinarService veterinar, IDyrService dyr)
    {
        _veterinar = veterinar;
        _dyr = dyr;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var idag = DateOnly.FromDateTime(DateTime.Now);
        var besok = await _veterinar.HentBesok(ct);

        return View(new VeterinarSideVm
        {
            Steder = await _veterinar.Hent(ct),

            // Kommende sorteres stigende - den neste er den viktigste, og
            // skal sta oeverst. Tidligere sorteres motsatt.
            Kommende = besok
                .Where(b => b.ErKommende(idag))
                .OrderBy(b => b.Dato)
                .ThenBy(b => b.Klokkeslett ?? TimeOnly.MinValue)
                .ToList(),

            Tidligere = besok.Where(b => !b.ErKommende(idag)).ToList(),

            BetaltIArKr = besok
                .Where(b => b.Dato.Year == idag.Year)
                .Sum(b => b.NettoKr ?? 0),

            RefundertIArKr = besok
                .Where(b => b.Dato.Year == idag.Year)
                .Sum(b => b.RefundertKr ?? 0)
        });
    }

    // --- Steder -------------------------------------------------------------

    /// <summary>
    /// Alt som er registrert pa ett sted.
    ///
    /// Nettside, e-post, apningstider og notat ble lagret, men vistes ingen
    /// steder utenom redigeringsskjemaet - man matte apne "Endre" for a lese
    /// sin egen nettadresse.
    ///
    /// Ingen [KreverEier]: dette er lesing, og en gjest skal kunne finne
    /// nummeret til vakten like godt som den som bor der.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detaljer(int id, CancellationToken ct)
    {
        var rad = await _veterinar.HentEn(id, ct);

        if (rad is null)
        {
            return NotFound();
        }

        // Uten skript apnes ingen dialog, og da skal svaret vaere en vanlig
        // side. Derfor er raden en lenke med href, ikke en bar knapp.
        return Request.Headers.ContainsKey("HX-Request")
            ? PartialView("_Detaljer", rad)
            : View("Detaljer", rad);
    }

    [HttpGet("ny")]
    [KreverEier]
    public IActionResult Ny() => View(Skjema, new NyVeterinarVm());

    [HttpGet("{id:int}/rediger")]
    [KreverEier]
    public async Task<IActionResult> Rediger(int id, CancellationToken ct)
    {
        var rad = await _veterinar.HentEn(id, ct);

        if (rad is null)
        {
            return NotFound();
        }

        return View(Skjema, new NyVeterinarVm
        {
            Id = rad.Id,
            Navn = rad.Navn,
            Type = rad.Type,
            Telefon = rad.Telefon,
            Adresse = rad.Adresse,
            Nettside = rad.Nettside,
            Epost = rad.Epost,
            ApentMandag = rad.Apningstider.Mandag,
            ApentTirsdag = rad.Apningstider.Tirsdag,
            ApentOnsdag = rad.Apningstider.Onsdag,
            ApentTorsdag = rad.Apningstider.Torsdag,
            ApentFredag = rad.Apningstider.Fredag,
            ApentLordag = rad.Apningstider.Lordag,
            ApentSondag = rad.Apningstider.Sondag,
            Notat = rad.Notat
        });
    }

    [HttpPost("")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lagre(NyVeterinarVm ny, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(Skjema, ny);
        }

        var input = new NyVeterinar(
            ny.Navn, ny.Type, ny.Telefon, ny.Adresse,
            ny.Nettside, ny.Epost,
            new Apningstider(
                ny.ApentMandag, ny.ApentTirsdag, ny.ApentOnsdag,
                ny.ApentTorsdag, ny.ApentFredag, ny.ApentLordag,
                ny.ApentSondag),
            ny.Notat);

        var ok = ny.Id is { } id
            ? await _veterinar.Oppdater(id, input, ct)
            : await _veterinar.Opprett(input, ct);

        if (!ok)
        {
            return NotFound();
        }

        TempData["Melding"] = ny.Id is null
            ? $"{ny.Navn} er lagt til."
            : $"{ny.Navn} er oppdatert.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/slett")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Slett(int id, CancellationToken ct)
    {
        if (!await _veterinar.Slett(id, ct))
        {
            return NotFound();
        }

        // Sies eksplisitt: den som sletter et sted skal vite at loggen star
        // igjen, ikke lure pa om besokene forsvant med det.
        TempData["Melding"] = "Stedet er slettet. Tidligere besøk er beholdt.";
        return RedirectToAction(nameof(Index));
    }

    // --- Timer --------------------------------------------------------------

    [HttpGet("time/ny")]
    [KreverEier]
    public async Task<IActionResult> NyTime(CancellationToken ct)
        => View(Timeskjema, await ByggTime(new NyttVetbesokVm(), ct));

    [HttpGet("time/{id:int}/rediger")]
    [KreverEier]
    public async Task<IActionResult> RedigerTime(int id, CancellationToken ct)
    {
        var rad = (await _veterinar.HentBesok(ct)).SingleOrDefault(b => b.Id == id);

        if (rad is null)
        {
            return NotFound();
        }

        return View(Timeskjema, await ByggTime(new NyttVetbesokVm
        {
            Id = rad.Id,
            DyrId = rad.DyrId,
            VeterinarId = rad.VeterinarId,
            Klinikk = rad.Klinikk,
            Dato = rad.Dato,
            Klokkeslett = rad.Klokkeslett,
            Arsak = rad.Arsak,
            Diagnose = rad.Diagnose,
            KostnadKr = rad.KostnadKr,
            ForsikringKrevd = rad.ForsikringKrevd,
            RefundertKr = rad.RefundertKr,
            NesteKontrollDato = rad.NesteKontrollDato,
            Notat = rad.Notat
        }, ct));
    }

    [HttpPost("time")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LagreTime(
        NyttVetbesokVm ny, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(Timeskjema, await ByggTime(ny, ct));
        }

        var input = new NyttVetbesok(
            ny.DyrId, ny.VeterinarId, ny.Klinikk, ny.Dato, ny.Klokkeslett,
            ny.Arsak, ny.Diagnose, ny.KostnadKr, ny.ForsikringKrevd,
            ny.RefundertKr, ny.NesteKontrollDato, ny.Notat);

        var ok = ny.Id is { } id
            ? await _veterinar.OppdaterBesok(id, input, ct)
            : await _veterinar.OpprettBesok(input, ct);

        if (!ok)
        {
            return NotFound();
        }

        TempData["Melding"] = ny.Id is null
            ? "Timen er lagt inn."
            : "Timen er oppdatert.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("time/{id:int}/slett")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SlettTime(int id, CancellationToken ct)
    {
        if (!await _veterinar.SlettBesok(id, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Timen er slettet.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Visningsnavn som konstanter. Skrivefeil i en strengbokstav gir en
    /// kjoretidsfeil, ikke en byggefeil.
    /// </summary>
    private const string Skjema = "Skjema";

    private const string Timeskjema = "Timeskjema";

    private async Task<VetbesokSkjemaVm> ByggTime(
        NyttVetbesokVm ny, CancellationToken ct)
        => new()
        {
            Ny = ny,
            DyrValg = (await _dyr.HentAlle(ct))
                .Select(d => new SelectListItem(d.Navn, d.Id.ToString()))
                .ToList(),
            StedValg = (await _veterinar.Hent(ct))
                .Select(v => new SelectListItem(v.Navn, v.Id.ToString()))
                .ToList()
        };
}
