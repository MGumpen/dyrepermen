using System.Net;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Forplansiden viste dagsmengde og porsjon, men ikke hvor tallet kom fra.
/// En bruker som ser "410 g per dag" skal kunne se om det er 5 % av vekten
/// eller en fast mengde noen skrev inn - ellers ma hun apne skjemaet og
/// gjette seg fram.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class ForplangrunnlagTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public ForplangrunnlagTester(DatabaseFixture fixture) => _fixture = fixture;

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

    /// <summary>
    /// Henter siden og dekoder HTML-entitetene.
    ///
    /// Standardencoderen i ASP.NET Core koder ALT utenfor ASCII, sa "Folger"
    /// med o-med-strek star i kilden som &#xF8;. En assertion pa norsk tekst
    /// feiler da pa en side som ser helt riktig ut i nettleseren - og bare
    /// pa ordene som har aeoa, sa halve testen bestar og halve feiler.
    /// </summary>
    private static async Task<string> Side(Skjemaklient klient, string sti)
        => WebUtility.HtmlDecode(
            await (await klient.Hent(sti)).Content.ReadAsStringAsync());

    private async Task<(Skjemaklient klient, int dyrId)> DyrMedForplan()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);
        await Testoppsett.SlaPaForingslogg(klient, dyrId);
        return (klient, dyrId);
    }

    /// <summary>Legger inn en vekt i kilo, slik skjemaet tar imot den.</summary>
    private static async Task LeggInnVekt(Skjemaklient klient, int dyrId, string kilo)
    {
        var svar = await klient.Post(
            $"/dyr/{dyrId}/vekt",
            new Dictionary<string, string>
            {
                // Komma, ikke punktum. Kulturen er fast nb-NO.
                ["Kilo"] = kilo,
                ["Dato"] = "2026-08-01"
            },
            tokenFra: $"/dyr/{dyrId}/vekt");

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Vekten ble ikke lagret: {await Skjemaklient.Feilmeldinger(svar)}");
    }

    private static async Task ProsentPlan(
        Skjemaklient klient, int dyrId, string prosent, int maltider)
    {
        var svar = await klient.Post(
            $"/dyr/{dyrId}/forplan",
            new Dictionary<string, string>
            {
                ["Metode"] = ((int)Formetode.Prosent).ToString(),
                ["Prosent"] = prosent,
                ["AntallMaltider"] = maltider.ToString()
            },
            tokenFra: $"/dyr/{dyrId}/forplan");

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Forplanen ble ikke lagret: {await Skjemaklient.Feilmeldinger(svar)}");
    }

    [Fact]
    public async Task Prosentplan_viser_regelen_og_hele_regnestykket()
    {
        var (klient, dyrId) = await DyrMedForplan();
        await LeggInnVekt(klient, dyrId, "8,2");
        await ProsentPlan(klient, dyrId, "5,0", 2);

        var html = await Side(klient, $"/dyr/{dyrId}/forplan");

        // Regelen selv, ikke bare resultatet.
        Assert.Contains("5 % av kroppsvekten", html);

        // Begge leddene i regnestykket, sa brukeren kan regne etter.
        Assert.Contains("8,20 kg", html);
        Assert.Contains("410 g", html);

        // Og at planen er levende.
        Assert.Contains("Følger vekten", html);
    }

    [Fact]
    public async Task Fast_mengde_sier_at_den_ikke_folger_vekten()
    {
        var (klient, dyrId) = await DyrMedForplan();
        await Testoppsett.ForplanIGram(klient, dyrId, 400, 2);

        var html = await Side(klient, $"/dyr/{dyrId}/forplan");

        Assert.Contains("Fast mengde", html);
        Assert.Contains("400 g per dag", html);

        // Prosentregelen skal ikke sta pa en plan som ikke bruker den.
        Assert.DoesNotContain("av kroppsvekten", html);
    }

    /// <summary>
    /// Den viktigste av de tre. Uten vekt kan mengden ikke regnes ut, men
    /// regelen er lagt inn og skal vises - ellers star brukeren igjen med en
    /// advarsel og ingen anelse om hva planen faktisk sier.
    /// </summary>
    [Fact]
    public async Task Prosentplan_uten_vekt_viser_regelen_likevel()
    {
        var (klient, dyrId) = await DyrMedForplan();
        await ProsentPlan(klient, dyrId, "4,0", 2);

        var html = await Side(klient, $"/dyr/{dyrId}/forplan");

        Assert.Contains("Registrer en vekt", html);
        Assert.Contains("4 % av kroppsvekten", html);
    }

    /// <summary>
    /// Maltidene kan fa ulikt antall gram fordi resten legges pa de forste.
    /// Uten forklaringen ser 134 + 134 + 133 ut som en regnefeil.
    /// </summary>
    [Fact]
    public async Task Ujevn_fordeling_er_forklart()
    {
        var (klient, dyrId) = await DyrMedForplan();
        await Testoppsett.ForplanIGram(klient, dyrId, 401, 3);

        var html = await Side(klient, $"/dyr/{dyrId}/forplan");

        Assert.Contains("134 g", html);
        Assert.Contains("133 g", html);
        Assert.Contains("legges grammene som blir til overs", html);
    }
}
