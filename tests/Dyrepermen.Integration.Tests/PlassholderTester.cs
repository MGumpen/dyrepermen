using System.Net;
using System.Text.RegularExpressions;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Tallfelter skal sta TOMME med plassholder, ikke ferdig utfylt med et tall
/// brukeren ma viske ut for hun kan skrive.
///
/// Feilen var at ViewModel-ene brukte int framfor int?. Et int-felt uten
/// verdi er 0, og asp-for skriver den ut som value="0". Det ser ut som en
/// utfylt verdi, og i hvert eneste felt matte nullen markeres og slettes
/// forst.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class PlassholderTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public PlassholderTester(DatabaseFixture fixture) => _fixture = fixture;

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

    /// <summary>Henter value-attributtet til et navngitt inputfelt.</summary>
    private static string? Verdi(string html, string navn)
    {
        var tag = Regex.Match(html, $"<input[^>]*name=\"{Regex.Escape(navn)}\"[^>]*>");
        if (!tag.Success)
        {
            return null;
        }

        var verdi = Regex.Match(tag.Value, "value=\"([^\"]*)\"");
        return verdi.Success ? verdi.Groups[1].Value : "";
    }

    [Theory]
    [InlineData("/forsikring", "Ny.ArspremieKr")]
    [InlineData("/forsikring", "Ny.ForsikringsbelopKr")]
    [InlineData("/forsikring", "Ny.EgenandelFastKr")]
    [InlineData("/forsikring", "Ny.EgenandelVariabelProsent")]
    public async Task Tomt_skjema_har_ingen_utfylte_tall(string sti, string felt)
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        await Testoppsett.NyttDyr(klient, "Luna");

        var svar = await klient.Hent(sti);
        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);

        var html = await svar.Content.ReadAsStringAsync();

        Assert.NotNull(Verdi(html, felt));
        Assert.Equal("", Verdi(html, felt));
    }

    [Theory]
    [InlineData("medisin", "Ny.IntervallTimer")]
    [InlineData("forplan", "Ny.AntallMaltider")]
    public async Task Tomt_skjema_pa_underside_har_ingen_utfylte_tall(
        string underside, string felt)
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient, "Luna");

        var html = await (await klient.Hent($"/dyr/{dyrId}/{underside}"))
            .Content.ReadAsStringAsync();

        Assert.Equal("", Verdi(html, felt));
    }

    [Fact]
    public async Task Redigering_fyller_fortsatt_inn_de_lagrede_tallene()
    {
        // Motprøven. Tomme felter skal gjelde NYE skjemaer - apner man en
        // lagret forsikring for a endre den, ma tallene sta der.
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient, "Luna");

        await klient.Post("/forsikring", new Dictionary<string, string>
        {
            ["DyrId"] = dyrId.ToString(),
            ["Selskap"] = "Gjensidige",
            ["ArspremieKr"] = "4200",
            ["ForsikringsbelopKr"] = "60000",
            ["EgenandelFastKr"] = "1500",
            ["EgenandelVariabelProsent"] = "20"
        });

        var html = await (await klient.Hent("/forsikring"))
            .Content.ReadAsStringAsync();

        var id = Regex.Match(html, @"/forsikring/(\d+)/rediger").Groups[1].Value;
        Assert.NotEqual("", id);

        var skjema = await (await klient.Hent($"/forsikring/{id}/rediger"))
            .Content.ReadAsStringAsync();

        Assert.Equal("4200", Verdi(skjema, "Ny.ArspremieKr"));
        Assert.Equal("1500", Verdi(skjema, "Ny.EgenandelFastKr"));
    }

    [Fact]
    public async Task Tomt_antall_pa_handlelisten_blir_ett()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);

        // Rent ASCII i navnet med vilje. Razor HTML-koder uttrykksverdier, sa
        // "Tørrfôr" star som "T&#xF8;rrf&#xF4;r" i kilden - og da ville
        // testen feilet pa tegnsetting i stedet for pa det den maler.
        var svar = await klient.Post("/handleliste", new Dictionary<string, string>
        {
            ["tekst"] = "Torrfor",
            ["antall"] = ""
        });

        Assert.NotEqual(HttpStatusCode.InternalServerError, svar.StatusCode);

        var html = await (await klient.Hent("/handleliste"))
            .Content.ReadAsStringAsync();

        Assert.Contains("Torrfor", html);

        // Antallet vises kun nar det er mer enn 1. Sto det "x 0" eller "x 2",
        // var tomt felt tolket som noe annet enn ett.
        Assert.DoesNotContain("× 0", html);
        Assert.DoesNotContain("× 2", html);
    }
}
