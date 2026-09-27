using Dyrepermen.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Dyrepermen.Web.Controllers;

/// <summary>
/// Serverer opplastede filer. Aldri fra wwwroot: hver fil gar gjennom
/// query-filteret, sa en id fra en annen husstand gir 404 - det samme svaret
/// som en id som ikke finnes. Se plan kapittel 15.
/// </summary>
[Route("dokument")]
public sealed class DokumentController : Controller
{
    private readonly IDokumentService _dokumenter;

    public DokumentController(IDokumentService dokumenter) => _dokumenter = dokumenter;

    /// <summary>
    /// Vises i nettleseren, ikke lastet ned: det er kvitteringen man vil se.
    /// Det er trygt fordi typen ble avgjort av innholdet ved opplasting - bare
    /// jpeg, png og pdf slipper inn - og nosniff er satt for alle svar, sa
    /// nettleseren ikke gjetter en annen type.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Vis(int id, CancellationToken ct)
    {
        var fil = await _dokumenter.Hent(id, ct);

        if (fil is null)
        {
            return NotFound();
        }

        var visning = new ContentDispositionHeaderValue("inline");
        visning.SetHttpFileName(fil.Navn);
        Response.Headers.ContentDisposition = visning.ToString();

        // Privat: filen skal ikke ligge i en delt mellomlagring.
        Response.Headers.CacheControl = "private, max-age=3600";

        return File(fil.Data, fil.Innholdstype);
    }
}
