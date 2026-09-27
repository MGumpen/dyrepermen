using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Web.Extensions;
using Dyrepermen.Web.Filtre;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Samleside: alt vi vet om hvert dyr, pluss frie notater. Siden du kan
/// vise fram til dyrepasseren.
/// </summary>
[Route("informasjon")]
public sealed class InformasjonController : Controller
{
    private readonly IInformasjonService _info;
    private readonly IDyrService _dyr;
    private readonly IUtskriftService _utskrift;

    public InformasjonController(
        IInformasjonService info, IDyrService dyr, IUtskriftService utskrift)
    {
        _info = info;
        _dyr = dyr;
        _utskrift = utskrift;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await Bygg(new NyttNotatVm(), ct));

    /// <summary>
    /// Velg hvilke dyr og hvilke deler som skal med pa utskriften.
    ///
    /// Tar samme parametre som <see cref="Utskrift"/>, slik at "Endre
    /// utvalg" pa utskriften kommer tilbake hit med det som var valgt. Uten
    /// parametre er alt krysset av.
    /// </summary>
    [HttpGet("utskrift/velg")]
    public async Task<IActionResult> Velg(
        [FromQuery] int[] dyr, [FromQuery] Utskriftsdel[] del, bool valgt,
        CancellationToken ct)
        => View(await ByggValg(valgt ? Valg(dyr, del) : Utskriftsvalg.Alt, ct));

    /// <summary>
    /// Utskriftsvennlig samleside for de valgte dyrene.
    ///
    /// Egen side og ikke en @media print-regel pa Index: utskriften har et
    /// annet innhold enn skjermen, ikke bare et annet utseende. Den tar med
    /// vekthistorikk, behandlinger og forsikringer som ikke star der, og
    /// utelater notatskjemaet og navigasjonen.
    ///
    /// Utvalget ligger i adressen, ikke i et POST-skjema. Det er en lesing,
    /// ikke en endring - og da kan siden lastes pa nytt, bokmerkes og deles
    /// uten at noe sendes pa nytt. <paramref name="valgt"/> skiller et skjema
    /// der ingenting er krysset av, fra en lenke uten utvalg: det forste er en
    /// feil, det andre betyr hele permen.
    /// </summary>
    [HttpGet("utskrift")]
    public async Task<IActionResult> Utskrift(
        [FromQuery] int[] dyr, [FromQuery] Utskriftsdel[] del, bool valgt,
        CancellationToken ct)
    {
        var utvalg = valgt ? Valg(dyr, del) : Utskriftsvalg.Alt;

        if (utvalg.Deler == Utskriftsdel.Ingen)
        {
            ModelState.AddModelError(string.Empty, "Velg minst én del som skal med.");
        }
        else if (utvalg.DyrIder is [] && !utvalg.Har(Utskriftsdel.FellesNotater))
        {
            ModelState.AddModelError(string.Empty, "Velg minst ett dyr.");
        }

        if (!ModelState.IsValid)
        {
            return View(nameof(Velg), await ByggValg(utvalg, ct));
        }

        // Samme sporrestreng tilbake til valgsiden, sa "Endre utvalg" apner
        // skjemaet med det som var krysset av.
        ViewData["EndreUtvalg"] = Url.Action(nameof(Velg)) + Request.QueryString;

        return View(await _utskrift.Hent(utvalg, ct));
    }

    private static Utskriftsvalg Valg(int[] dyr, Utskriftsdel[] del)
        => new(dyr, del.Aggregate(Utskriftsdel.Ingen, (sum, d) => sum | d));

    private async Task<UtskriftsvalgVm> ByggValg(
        Utskriftsvalg valg, CancellationToken ct)
        => new()
        {
            Dyr = (await _dyr.HentAlle(ct))
                .Select(d => new SelectListItem(
                    d.Navn, d.Id.ToString(),
                    valg.DyrIder is null || valg.DyrIder.Contains(d.Id)))
                .ToList(),
            Deler = Utskriftsformat.Deler
                .Select(d => new SelectListItem(
                    Utskriftsformat.Navn(d), d.ToString(), valg.Har(d)))
                .ToList()
        };

    [HttpGet("{id:int}/rediger")]
    public async Task<IActionResult> Rediger(int id, CancellationToken ct)
    {
        var rad = await _info.HentEn(id, ct);
        if (rad is null)
        {
            return NotFound();
        }

        return View(nameof(Index), await Bygg(new NyttNotatVm
        {
            Id = rad.Id,
            Tittel = rad.Tittel,
            Tekst = rad.Tekst,
            DyrId = rad.DyrId
        }, ct));
    }

    [HttpPost("")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lagre(NyttNotatVm ny, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), await Bygg(ny, ct));
        }

        var ok = await _info.Lagre(new NyInformasjon(
            ny.Id, ny.Tittel, ny.Tekst, ny.DyrId, User.BrukerId()), ct);

        if (!ok)
        {
            return NotFound();
        }

        TempData["Melding"] = ny.Id is null
            ? "Notatet er lagret."
            : "Notatet er oppdatert.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/slett")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Slett(int id, CancellationToken ct)
    {
        if (!await _info.Slett(id, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Notatet er slettet.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<InformasjonVm> Bygg(NyttNotatVm ny, CancellationToken ct)
    {
        var alle = await _info.Hent(ct);

        return new InformasjonVm
        {
            Dyr = await _info.HentDyreoversikt(ct),
            FellesNotater = alle.Where(n => n.DyrId is null).ToList(),
            DyrValg = (await _dyr.HentAlle(ct))
                .Select(d => new SelectListItem(d.Navn, d.Id.ToString()))
                .ToList(),
            Ny = ny
        };
    }
}
