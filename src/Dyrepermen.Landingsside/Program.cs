// Landingsside for main, mens appen bygges pa dev.
//
// Dette er BEVISST en annen Program.cs enn appens. Appen apner database,
// kjorer migrasjoner ved oppstart, setter opp Identity og Data Protection.
// Ingen av delene hoerer hjemme her: en side som sier "under utvikling" skal
// komme opp selv om det ikke finnes noen database i det hele tatt.
//
// Konsekvensen er at Render ikke trenger en eneste miljovariabel for denne
// branchen. Ingen tilkoblingsstreng, ingen hemmeligheter, ingenting som kan
// vaere feil.

var builder = WebApplication.CreateBuilder(args);

// Render tildeler porten gjennom PORT. Uten dette lytter appen pa 8080 og
// Render finner den ikke - tjenesten blir staende og vente pa en port som
// aldri apnes. Faller tilbake til 8080, som er det Dockerfile eksponerer.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

var app = builder.Build();

// Ingen UseHttpsRedirection. Render terminerer TLS i sin egen proxy og
// sender http videre; en omdirigering her ville sendt brukeren i ring.
// Appen trenger ForwardedHeaders av samme grunn - landingssiden har ingen
// innlogging og ingen omdirigeringer, sa den slipper.

// Ingen mellomlagring. Siden skal byttes ut med appen, og da skal ingen
// sitte igjen med "under utvikling" fordi nettleseren holder pa en gammel
// kopi.
//
// Headeren settes her og ikke i StaticFileOptions.OnPrepareResponse, som
// var forste forsok. Den fungerer bare for filer UseStaticFiles selv
// serverer - og /  betjenes av MapFallbackToFile, som har sin egen
// filhandtering og ikke bruker de opsjonene. Resultatet var at /index.html
// fikk headeren mens / ikke fikk den, altsa nettopp adressen folk apner.
app.Use(async (kontekst, neste) =>
{
    kontekst.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
    await neste();
});

app.UseDefaultFiles();
app.UseStaticFiles();

// Samme kontrakt som appen: 200 uten a treffe databasen. Da kan Render
// bruke samme helsesjekk uansett hvilken branch tjenesten star pa.
app.MapGet("/helse", () => Results.Ok(new
{
    status = "ok",
    rolle = "landingsside",
    tid = DateTimeOffset.UtcNow
}));

// Alt annet er landingssiden. Uten dette gir /noe-som-helst en tom 404 -
// og en blank side ser ut som at tjenesten er nede.
app.MapFallbackToFile("index.html");

app.Run();
