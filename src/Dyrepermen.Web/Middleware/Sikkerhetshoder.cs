namespace Dyrepermen.Web.Middleware;

/// <summary>
/// Sikkerhetshoder pa hvert svar, ogsa statiske filer og feilsider.
///
/// VAER AERLIG OM HVA DENNE CSP-EN GJOR: den stopper ikke skriptinnsprøyting
/// i markupen, fordi appen har inline script-blokker, inline onclick og
/// hx-on fra htmx. Derfor ma 'unsafe-inline' og 'unsafe-eval' sta.
///
/// Det den DOES stoppe er likevel verdt a ha:
///   - skript, stiler og bilder fra andre opphav (default-src 'self')
///   - skjema som sendes til en fremmed server (form-action)
///   - at appen legges i en ramme pa et annet nettsted (frame-ancestors)
///   - at en innsprøytet base-tagg flytter alle relative URL-er
///   - plugin-innhold (object-src)
///
/// Razor koder ut automatisk, sa risikoen for XSS er lav i utgangspunktet.
/// Skal 'unsafe-inline' bort, ma hendelseshandtererne ut av markupen og inn i
/// egne filer, og script-blokkene ma fa nonce. Det er en reell jobb, ikke en
/// bryter - den er notert, ikke gjort.
/// </summary>
public sealed class Sikkerhetshoder
{
    private readonly RequestDelegate _neste;

    public Sikkerhetshoder(RequestDelegate neste) => _neste = neste;

    private const string Retningslinje =
        "default-src 'self'; "
        // unsafe-inline: inline script-blokker og onclick i visningene.
        // unsafe-eval: htmx kompilerer hx-on-uttrykk med new Function.
        + "script-src 'self' 'unsafe-inline' 'unsafe-eval'; "
        // Inline <style> i layouten, og style-attributter i flere visninger.
        + "style-src 'self' 'unsafe-inline'; "
        + "img-src 'self' data:; "
        + "font-src 'self'; "
        + "connect-src 'self'; "
        // Et innsprøytet skjema skal ikke kunne poste passordet ut av huset.
        + "form-action 'self'; "
        // Klikkjacking. X-Frame-Options under dekker eldre nettlesere.
        + "frame-ancestors 'none'; "
        + "base-uri 'none'; "
        + "object-src 'none'";

    public Task InvokeAsync(HttpContext kontekst)
    {
        var hoder = kontekst.Response.Headers;

        // Uten nosniff kan en fil vi serverer som tekst bli tolket som skript
        // fordi innholdet ser slik ut.
        hoder["X-Content-Type-Options"] = "nosniff";
        hoder["X-Frame-Options"] = "DENY";

        // Full URL skal ikke folge med til andre nettsteder. Rutene vare
        // inneholder ID-er, og /dyr/7 i en fremmed serverlogg er unodvendig.
        hoder["Referrer-Policy"] = "strict-origin-when-cross-origin";

        hoder["Content-Security-Policy"] = Retningslinje;

        return _neste(kontekst);
    }
}
