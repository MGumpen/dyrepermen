using System.Net;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Ordet "polise" skal ikke vises til brukeren - det heter forsikring, og
/// nummeret heter forsikringsnummer.
///
/// Denne testen finnes fordi ordet slapp gjennom TO ganger: forst sto det
/// igjen i listeraden og i overskriften "Endre polise" etter at etiketten i
/// skjemaet var byttet. Egenskapene i koden heter fortsatt PoliseNr og
/// Poliser - det er kun teksten pa skjermen dette gjelder.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class ForsikringstekstTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public ForsikringstekstTester(DatabaseFixture fixture) => _fixture = fixture;

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

    private static async Task LeggInnForsikring(Skjemaklient klient, int dyrId)
    {
        var svar = await klient.Post("/forsikring", new Dictionary<string, string>
        {
            ["DyrId"] = dyrId.ToString(),
            ["Selskap"] = "Agria",
            ["PoliseNr"] = "9342645-001",
            ["ArspremieKr"] = "8102",
            ["ForsikringsbelopKr"] = "90000",
            ["EgenandelFastKr"] = "4000",
            ["EgenandelVariabelProsent"] = "15"
        });

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Forsikringen ble ikke lagret: {await Skjemaklient.Feilmeldinger(svar)}");
    }

    [Fact]
    public async Task Forsikringssiden_sier_forsikringsnummer_ikke_polise()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient, "Luna");
        await LeggInnForsikring(klient, dyrId);

        var svar = await klient.Hent("/forsikring");
        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);

        var html = await svar.Content.ReadAsStringAsync();

        Assert.Contains("Forsikringsnummer", html);
        Assert.Contains("9342645-001", html);
        Assert.Contains("Aktive forsikringer", html);

        Assert.DoesNotContain("Polise ", html);
        Assert.DoesNotContain("polise", html);
    }

    [Fact]
    public async Task Redigeringsskjemaet_heter_Endre_forsikring()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient, "Luna");
        await LeggInnForsikring(klient, dyrId);

        var liste = await (await klient.Hent("/forsikring")).Content.ReadAsStringAsync();
        var id = System.Text.RegularExpressions.Regex
            .Match(liste, @"/forsikring/(\d+)/rediger").Groups[1].Value;
        Assert.NotEqual("", id);

        var html = await (await klient.Hent($"/forsikring/{id}/rediger"))
            .Content.ReadAsStringAsync();

        Assert.Contains("Endre forsikring", html);
        Assert.DoesNotContain("polise", html);
    }

    [Fact]
    public async Task Utskriften_sier_ogsa_forsikringsnummer()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient, "Luna");
        await LeggInnForsikring(klient, dyrId);

        var html = await (await klient.Hent("/informasjon/utskrift"))
            .Content.ReadAsStringAsync();

        Assert.Contains("Forsikringsnummer", html);
        Assert.DoesNotContain("polise", html);
    }
}
