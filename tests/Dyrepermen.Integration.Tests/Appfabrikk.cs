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
                ["ConnectionStrings:Postgres"] = _tilkobling
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
