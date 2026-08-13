using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Starter den EKTE appen mot testcontaineren.
///
/// Poenget er at ingenting byttes ut: samme Program.cs, samme middleware,
/// samme Identity-oppsett, samme passordregler. En egen testoppstart ville
/// testet noe annet enn det som kjorer i produksjon - og feilen vi retter her
/// var nettopp at to steder beskrev samme regel ulikt.
///
/// Migrasjonene kjores ved oppstart (ADR 0010), sa skjemaet kommer pa plass
/// av seg selv.
/// </summary>
public sealed class Appfabrikk : WebApplicationFactory<Program>
{
    private readonly string _tilkobling;

    public Appfabrikk(string tilkobling) => _tilkobling = tilkobling;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(cfg =>
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _tilkobling,

                // Testklienten snakker http mot localhost. Med
                // CookieSecurePolicy.Always merkes innloggingskapselen
                // Secure, og da lagrer ikke klienten den - foelgelig er hver
                // eneste foresporsel etter innlogging uinnlogget, og en test
                // som ber om dashbordet far 302 til innloggingssiden.
                //
                // Samme lever som web-tjenesten i infra/compose.yaml bruker,
                // og av samme grunn: ingen TLS-terminator foran. Den er
                // eksplisitt konfigurasjon nettopp for a kunne settes her,
                // og applikasjonen nekter a starte med den av i Production.
                ["Sikkerhet:KrevSikkerKapsel"] = "false"
            }));

        return base.CreateHost(builder);
    }

    /// <summary>
    /// Klient som IKKE folger omdirigeringer.
    ///
    /// Det er selve malingen i disse testene: en vellykket registrering
    /// svarer 302, en avvist svarer 200 med skjemaet og feilmeldingen. Folger
    /// klienten omdirigeringen, ser begge like ut.
    /// </summary>
    public HttpClient LagKlient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true
    });
}
