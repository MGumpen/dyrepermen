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
        var vm = await ByggSide(dyrId, ny: null, redigerId: rediger, ct, gjenta);
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
            var vm = await ByggSide(dyrId, ny, redigerId: null, ct);
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
            var vm = await ByggSide(dyrId, ny, redigerId: behandlingId, ct);
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
    /// Krysser av en paminnelse som gitt i dag. Ett trykk: behandlingen
    /// registreres med samme type og preparat, og neste gang med samme
    /// intervall som sist. Datoen appen foreslar, star i bekreftelsen, og
    /// kan rettes pa behandlingssiden. Se ADR 0016.
    ///
    /// <paramref name="fraOversikt"/> sender brukeren tilbake til dashbordet,
    /// der knappen sto. Se <see cref="MedisinController.LoggDose"/>.
    /// </summary>
    [HttpPost("{behandlingId:int}/gitt")]
    [KreverEier]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Gitt(
        int dyrId, int behandlingId, bool fraOversikt, CancellationToken ct)
    {
        var resultat = await _behandling.Gitt(dyrId, behandlingId, ct);

        switch (resultat.Status)
        {
            case Gittstatus.FinnesIkke:
                return NotFound();

            case Gittstatus.AlleredeFulgtOpp:
                // Ikke en feil brukeren har gjort. Som regel har noen andre i
                // husstanden krysset av forst, og det riktige er a si det.
                TempData["Feil"] = "Behandlingen er allerede registrert som gitt.";
                break;

            default:
                TempData["Melding"] = resultat.NesteDato is { } neste
                    ? $"{resultat.Beskrivelse} er registrert som gitt i dag. "
                      + $"Neste gang er satt til {neste:d. MMMM yyyy}."
                    : $"{resultat.Beskrivelse} er registrert som gitt i dag.";
                break;
        }

        return fraOversikt
            ? RedirectToAction(nameof(HjemController.Index), "Hjem")
            : RedirectToAction(nameof(Index), new { dyrId });
    }

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
        int dyrId, NyBehandlingVm? ny, int? redigerId, CancellationToken ct,
        int? gjentaId = null)
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
