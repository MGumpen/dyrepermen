using System.Net;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Apningstidene lagres per ukedag og skal vises for de dagene som faktisk er
/// fylt ut - hverken flere eller faerre.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class ApningstidVisningTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public ApningstidVisningTester(DatabaseFixture fixture) => _fixture = fixture;

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

    /// <summary>Legger inn et sted som er apent mandag, onsdag og lordag.</summary>
    private static async Task LeggInnSted(Skjemaklient klient)
    {
        var svar = await klient.Post("/veterinar", new Dictionary<string, string>
        {
            ["Navn"] = "Vestkanten Dyreklinikk",
            ["Type"] = "0",
            ["Telefon"] = "55 12 34 56",
            ["ApentMandag"] = "08-16",
            ["ApentOnsdag"] = "10-14, 16-20",
            ["ApentLordag"] = "10-14"
            // Tirsdag, torsdag, fredag og sondag star tomme - altsa stengt.
        });

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Stedet ble ikke lagret: {await Skjemaklient.Feilmeldinger(svar)}");
    }

    [Fact]
    public async Task Veterinaersiden_viser_kun_dagene_som_er_fylt_ut()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        await LeggInnSted(klient);

        var svar = await klient.Hent("/veterinar");
        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);

        var html = await svar.Content.ReadAsStringAsync();

        // De tre utfylte dagene, med kortnavn i den smale listen.
        Assert.Contains("Man", html);
        Assert.Contains("08-16", html);
        Assert.Contains("Ons", html);
        Assert.Contains("10-14, 16-20", html);

        // De stengte dagene skal ikke sta noe sted. Tirsdag og torsdag har
        // kortnavn som ikke forekommer i annen tekst pa siden.
        Assert.DoesNotContain(">Tir<", html);
        Assert.DoesNotContain(">Tor<", html);
        Assert.DoesNotContain(">Fre<", html);
    }

    [Fact]
    public async Task Dashbordet_viser_de_samme_dagene()
    {
        // Samme regel to steder ville vaert to steder den kunne sprike.
        // Begge leser Apningstider.Utfylte.
        var klient = await Testoppsett.InnloggetKlient(_app);
        await LeggInnSted(klient);

        var html = await (await klient.Hent("/")).Content.ReadAsStringAsync();

        Assert.Contains("Man", html);
        Assert.Contains("08-16", html);
        Assert.DoesNotContain(">Tir<", html);
    }

    [Fact]
    public async Task Sted_uten_apningstider_viser_ingen_dager()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);

        var lagret = await klient.Post("/veterinar", new Dictionary<string, string>
        {
            ["Navn"] = "Akuttvakta",
            ["Type"] = "1",
            ["Telefon"] = "800 12 345"
        });

        Assert.True(Skjemaklient.GikkGjennom(lagret));

        var html = await (await klient.Hent("/veterinar")).Content.ReadAsStringAsync();

        Assert.Contains("Akuttvakta", html);
        Assert.DoesNotContain("apningsdag", html);
    }

    [Fact]
    public async Task Redigering_fyller_inn_dagene_som_er_lagret()
    {
        // Motprove: apner man stedet for a endre det, skal dagene sta der.
        var klient = await Testoppsett.InnloggetKlient(_app);
        await LeggInnSted(klient);

        // Id-en star i detaljlenken pa raden. "Endre" ligger ikke i listen
        // - den bor i kortet som apnes ved klikk.
        var liste = await (await klient.Hent("/veterinar")).Content.ReadAsStringAsync();
        var id = System.Text.RegularExpressions.Regex
            .Match(liste, @"href=""/veterinar/(\d+)""").Groups[1].Value;
        Assert.NotEqual("", id);

        var skjema = await (await klient.Hent($"/veterinar/{id}/rediger"))
            .Content.ReadAsStringAsync();

        Assert.Contains("value=\"08-16\"", skjema);
        Assert.Contains("value=\"10-14, 16-20\"", skjema);
    }
}
