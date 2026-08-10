using System.Security.Claims;

namespace Dyrepermen.Web.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Bruker-ID fra innloggingskapselen.
    ///
    /// Merk at husstand IKKE hentes fra claims. En claim satt ved innlogging
    /// blir foreldet nar et medlem legges til av noen andre, og serveren kan
    /// ikke oppdatere en annen brukers informasjonskapsel. Med 30 dagers
    /// vedvarende innlogging ville personen sett en tom applikasjon i ukevis.
    /// Husstand leses fra databasen i HusstandMiddleware. Plan kapittel 12.3.1.
    /// </summary>
    public static int? BrukerId(this ClaimsPrincipal bruker)
    {
        var verdi = bruker.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(verdi, out var id) ? id : null;
    }
}
