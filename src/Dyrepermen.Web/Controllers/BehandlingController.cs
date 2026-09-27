using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Web.Filtre;
using Dyrepermen.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Web.Controllers;

[Route("dyr/{dyrId:int}/behandling")]
public sealed class BehandlingController : Controller
{
    private readonly IBehandlingService _behandling;
    private readonly IDyrService _dyr;

    public BehandlingController(IBehandlingService behandling, IDyrService dyr)
    {
        _behandling = behandling;
        _dyr = dyr;
    }

    /// <summary>
    /// <paramref name="rediger"/> setter skjemaet i endringsmodus for den
    /// ene behandlingen. En sporrestrengparameter og ikke en egen side:
    /// historikken skal sta ved siden av mens man retter, sa man ser hva de
    /// andre radene sier.
    ///
    /// <paramref name="gjenta"/> fyller skjemaet fra en tidligere behandling
    /// i husstanden - type, preparat og intervallet til neste gang. Samme
    /// monster som rediger, men skjemaet registrerer en ny rad.
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> Index(
        int dyrId, int? rediger, int? gjenta, CancellationToken ct)
    {
        var vm = await ByggSide(dyrId, ny: null, redigerId: rediger, gjentaId: gjenta, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost("")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ny(
        int dyrId, NyBehandlingVm ny, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var vm = await ByggSide(dyrId, ny, redigerId: null, gjentaId: null, ct);
            return vm is null ? NotFound() : View(nameof(Index), vm);
        }

        var ok = await _behandling.Registrer(Innhold(dyrId, ny), ct);

        if (!ok)
        {
            return NotFound();
        }

        TempData["Melding"] = "Behandlingen er registrert.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    /// <summary>
    /// Retter en behandling som allerede star der.
    ///
    /// "Neste gang" er en avtale om framtiden, ikke et faktum om fortiden.
    /// Sier veterinaeren at ormekuren kan vente, skal datoen kunne flyttes
    /// uten at behandlingen som faktisk ble gitt ma slettes og legges inn pa
    /// nytt. Se ADR 0014.
    /// </summary>
    [HttpPost("{behandlingId:int}/rediger")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rediger(
        int dyrId, int behandlingId, NyBehandlingVm ny, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            // redigerId beholdes, sa skjemaet star igjen i endringsmodus med
            // feilmeldingene - ikke som et tomt registreringsskjema.
            var vm = await ByggSide(dyrId, ny, redigerId: behandlingId, gjentaId: null, ct);
            return vm is null ? NotFound() : View(nameof(Index), vm);
        }

        if (!await _behandling.Oppdater(behandlingId, Innhold(dyrId, ny), ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Behandlingen er oppdatert.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    /// <summary>
    /// Dialogen for "gitt i dag": hva som krysses av, og et tomt felt for
    /// neste gang. Tomt som standard - en dato appen har gjettet, blir lett
    /// staende uten at noen har tatt stilling til den. Se ADR 0016.
    ///
    /// Apnes i den felles dialogen med htmx. Uten skript havner man her som
    /// en vanlig side, med samme skjema - samme monster som foringsdialogen.
    /// </summary>
    [HttpGet("{behandlingId:int}/gitt")]
    [KreverEier]
    public async Task<IActionResult> Gittdialog(
        int dyrId, int behandlingId, bool fraOversikt, CancellationToken ct)
    {
        var grunnlag = await _behandling.HentGittgrunnlag(dyrId, behandlingId, ct);

        if (grunnlag is null)
        {
            return NotFound();
        }

        var vm = new GittdialogVm(grunnlag, NesteDato: null, fraOversikt);

        return ErHtmx
            ? PartialView("_Gittdialog", vm)
            : View("Gittdialog", vm);
    }

    /// <summary>
    /// Registrerer behandlingen som gitt i dag, med neste gang slik den sto
    /// i dialogen. Tomt felt betyr ingen ny paminnelse.
    /// </summary>
    [HttpPost("{behandlingId:int}/gitt")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Gitt(
        int dyrId, int behandlingId, GittVm skjema, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            // Vanlig side med feilmeldingen, ikke dialogen: skjemaet postes
            // uten htmx, og en side som svarer 200 med et skjema er det
            // nettleseren kan vise.
            var grunnlag = await _behandling.HentGittgrunnlag(dyrId, behandlingId, ct);

            return grunnlag is null
                ? NotFound()
                : View("Gittdialog", new GittdialogVm(
                    grunnlag, skjema.NesteDato, skjema.FraOversikt));
        }

        var resultat = await _behandling.Gitt(
            dyrId, behandlingId, skjema.NesteDato, ct);

        switch (resultat.Status)
        {
            case Gittstatus.FinnesIkke:
                return NotFound();

            case Gittstatus.AlleredeFulgtOpp:
                // Ikke en feil brukeren har gjort. Som regel har noen andre i
                // husstanden krysset av forst, og det riktige er a si det.
                TempData["Feil"] = "Behandlingen er allerede registrert som gitt.";
                break;

            case Gittstatus.GittIdag:
                TempData["Feil"] = "Behandlingen er allerede registrert i dag.";
                break;

            default:
                TempData["Melding"] = resultat.NesteDato is { } neste
                    ? $"{resultat.Beskrivelse} er registrert som gitt i dag. "
                      + $"Neste gang er {neste:d. MMMM yyyy}."
                    : $"{resultat.Beskrivelse} er registrert som gitt i dag.";
                break;
        }

        return skjema.FraOversikt
            ? RedirectToAction(nameof(HjemController.Index), "Hjem")
            : RedirectToAction(nameof(Index), new { dyrId });
    }

    private bool ErHtmx => Request.Headers.ContainsKey("HX-Request");

    private static Behandlingsinnhold Innhold(int dyrId, NyBehandlingVm ny)
        => new(dyrId, ny.Type, ny.Preparat, ny.Dato, ny.NesteDato, ny.Notat);

    [HttpPost("{behandlingId:int}/slett")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Slett(
        int dyrId, int behandlingId, CancellationToken ct)
    {
        if (!await _behandling.Slett(dyrId, behandlingId, ct))
        {
            return NotFound();
        }

        TempData["Melding"] = "Behandlingen er slettet.";
        return RedirectToAction(nameof(Index), new { dyrId });
    }

    /// <summary>
    /// <paramref name="ny"/> er null nar skjemaet ikke er sendt inn. Da
    /// fylles det fra behandlingen som skal endres, eller star tomt.
    /// </summary>
    private async Task<BehandlingSideVm?> ByggSide(
        int dyrId, NyBehandlingVm? ny, int? redigerId, int? gjentaId,
        CancellationToken ct)
    {
        var dyr = await _dyr.HentDetaljer(dyrId, ct);
        if (dyr is null)
        {
            return null;
        }

        var historikk = await _behandling.HentFor(dyrId, ct);

        // Behandlingen kan vaere slettet i en annen fane siden lenken ble
        // tegnet. Da faller siden tilbake til et tomt skjema i stedet for a
        // svare 404 pa en side som ellers er helt i orden.
        var rad = redigerId is { } id
            ? historikk.FirstOrDefault(b => b.Id == id)
            : null;

        var forslag = await _behandling.HentForslag(ct);

        // Samme tilbakefall som for rediger: et forslag som er slettet siden
        // lenken ble tegnet, gir et tomt skjema og ikke en feilside.
        var gjenta = rad is null && ny is null && gjentaId is { } g
            ? forslag.FirstOrDefault(f => f.Id == g)
            : null;

        return new BehandlingSideVm
        {
            DyrId = dyrId,
            DyrNavn = dyr.Navn,
            Historikk = historikk,
            RedigerId = rad?.Id,
            Forslag = forslag,
            FraForslag = gjenta is not null,
            Ny = ny ?? (gjenta is not null ? FraForslag(gjenta) : FraRad(rad))
        };
    }

    /// <summary>
    /// Dagens dato, og neste gang med samme intervall som forrige gang.
    /// Notatet folger ikke med - det handlet om den gangen, ikke denne.
    /// </summary>
    private static NyBehandlingVm FraForslag(Behandlingsforslag forslag)
    {
        var idag = Tidssone.Idag(DateTimeOffset.UtcNow);

        return new NyBehandlingVm
        {
            Type = forslag.Type,
            Preparat = forslag.Preparat,
            Dato = idag,
            NesteDato = Behandlingsintervall.NesteEtter(
                forslag.Dato, forslag.NesteDato, idag)
        };
    }

    private static NyBehandlingVm FraRad(BehandlingRad? rad)
        => rad is null
            ? new NyBehandlingVm()
            : new NyBehandlingVm
            {
                Type = rad.Type,
                Preparat = rad.Preparat,
                Dato = rad.Dato,
                NesteDato = rad.NesteDato,
                Notat = rad.Notat
            };
}
