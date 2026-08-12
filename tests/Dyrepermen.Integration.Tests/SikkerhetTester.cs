using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Sikkerhetsegenskaper som skal gjelde uansett hvem som legger til neste
/// controller. Alle kjores mot den ekte oppstarten.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class SikkerhetTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public SikkerhetTester(DatabaseFixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _app = new Appfabrikk(_fixture.Tilkobling);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _app.Dispose();
        return Task.CompletedTask;
    }

    // --- Antiforgery --------------------------------------------------------

    [Fact]
    public async Task POST_uten_antiforgery_token_avvises()
    {
        // Beviset over HTTP. En konfigurasjonstest kan si at filteret er
        // registrert; denne sier at det faktisk stopper noe.
        var svar = await _app.LagKlient().PostAsync("/logg-inn",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Epost"] = "noen@example.test",
                ["Passord"] = "Passord1",
                ["HuskMeg"] = "false"
            }));

        Assert.Equal(HttpStatusCode.BadRequest, svar.StatusCode);
    }

    [Fact]
    public void Antiforgery_kreves_globalt_ikke_bare_der_noen_husket_det()
    {
        // Uten dette filteret beskytter attributtene kun de handlingene som
        // allerede har dem. Nummer 40 ville statt apen, og ingenting hadde
        // sagt fra. Fjernes filteret, feiler denne.
        using var omfang = _app.Services.CreateScope();
        var valg = omfang.ServiceProvider
            .GetRequiredService<IOptions<MvcOptions>>().Value;

        Assert.Contains(valg.Filters,
            f => f is AutoValidateAntiforgeryTokenAttribute);
    }

    // --- Sikkerhetshoder ----------------------------------------------------

    [Theory]
    [InlineData("/logg-inn")]
    [InlineData("/css/site.css")]
    [InlineData("/helse")]
    public async Task Sikkerhetshoder_folger_alle_svar(string sti)
    {
        // Ogsa statiske filer og helsesjekken. Star middlewaren for langt ned
        // i pipelinen, mangler hodene nettopp der.
        var svar = await _app.LagKlient().GetAsync(sti);
        var hoder = svar.Headers;

        Assert.Equal("nosniff", hoder.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", hoder.GetValues("X-Frame-Options").Single());
        Assert.Equal("strict-origin-when-cross-origin",
            hoder.GetValues("Referrer-Policy").Single());
        Assert.True(hoder.Contains("Content-Security-Policy"));
    }

    [Fact]
    public async Task Retningslinjen_stenger_ramme_fremmed_skjemamal_og_base()
    {
        var svar = await _app.LagKlient().GetAsync("/logg-inn");
        var csp = svar.Headers.GetValues("Content-Security-Policy").Single();

        // Klikkjacking.
        Assert.Contains("frame-ancestors 'none'", csp);

        // Et innsprøytet skjema skal ikke kunne poste passordet ut av huset.
        Assert.Contains("form-action 'self'", csp);

        // En base-tagg ville flyttet alle relative URL-er til en fremmed vert.
        Assert.Contains("base-uri 'none'", csp);

        Assert.Contains("object-src 'none'", csp);
        Assert.Contains("default-src 'self'", csp);
    }

    // --- Fail closed --------------------------------------------------------

    [Theory]
    [InlineData("/")]
    [InlineData("/dyr")]
    [InlineData("/veterinar")]
    [InlineData("/handleliste")]
    [InlineData("/innstillinger")]
    [InlineData("/konto")]
    public async Task Sider_krever_innlogging(string sti)
    {
        // FallbackPolicy: alt uten [AllowAnonymous] er last. Glemmer noen a
        // sikre en ny controller, skal den vaere stengt - ikke apen.
        var svar = await _app.LagKlient().GetAsync(sti);

        Assert.Equal(HttpStatusCode.Found, svar.StatusCode);
        Assert.Contains("/logg-inn", svar.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData("/logg-inn")]
    [InlineData("/registrer")]
    [InlineData("/helse")]
    public async Task Kun_disse_er_apne_uten_innlogging(string sti)
    {
        var svar = await _app.LagKlient().GetAsync(sti);

        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
    }

    // --- Innloggingskapselen ------------------------------------------------

    [Fact]
    public async Task Innloggingskapselen_er_HttpOnly_og_SameSite()
    {
        // HttpOnly gjor at et skript ikke kan lese den. SameSite hindrer at
        // den folger med pa forespørsler fra andre nettsteder.
        //
        // Secure testes IKKE her: testverten kjorer http, og kapselen ville
        // aldri blitt satt. Den egenskapen er i stedet vernet av vakten i
        // Program.cs, som KASTER hvis den slas av i Production.
        var klient = new Skjemaklient(_app.LagKlient());
        var epost = $"kapsel-{Guid.NewGuid():N}@example.test";

        var svar = await klient.Post("/registrer", new Dictionary<string, string>
        {
            ["Epost"] = epost,
            ["Visningsnavn"] = "Kapseltest",
            ["Passord"] = "Passord1",
            ["BekreftPassord"] = "Passord1"
        });

        var kapsel = svar.Headers.GetValues("Set-Cookie")
            .Single(k => k.StartsWith("dyrepermen_auth", StringComparison.Ordinal));

        Assert.Contains("httponly", kapsel, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", kapsel, StringComparison.OrdinalIgnoreCase);
    }
}
