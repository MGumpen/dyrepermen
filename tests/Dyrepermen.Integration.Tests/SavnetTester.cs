using System.Net;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Savnet-plakaten og telefonnummeret som fylles inn pa den. Plakaten er en
/// egen utskrift fra informasjonssiden, og kommer ikke med i den vanlige.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class SavnetTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public SavnetTester(DatabaseFixture fixture) => _fixture = fixture;

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

    private static async Task<string> Tekst(Skjemaklient klient, string sti)
        => WebUtility.HtmlDecode(await (await klient.Hent(sti)).Content.ReadAsStringAsync());

    private static Task<HttpResponseMessage> LagreTelefon(Skjemaklient klient, string nummer)
        => klient.Post("/konto/telefon",
            new Dictionary<string, string> { ["Telefon.Nummer"] = nummer },
            tokenFra: "/konto");

    // --- Telefon ---------------------------------------------------------

    [Fact]
    public async Task Telefonnummeret_lagres_under_min_konto()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);

        // Uten nummer star skjemaet apent - det er ingenting a vise i raden.
        Assert.Contains("name=\"Telefon.Nummer\"", await Tekst(klient, "/konto"));

        Assert.True(Skjemaklient.GikkGjennom(await LagreTelefon(klient, "+47 412 34 567")));

        // Lagret nummer star som en rad, som navn og e-post - ikke i et felt.
        var etter = await Tekst(klient, "/konto");
        Assert.Contains("+47 412 34 567", etter);
        Assert.DoesNotContain("name=\"Telefon.Nummer\"", etter);
        Assert.Contains("href=\"/konto?endre=telefon#telefon\"", etter);

        // «Endre» apner skjemaet med nummeret fylt inn.
        Assert.Contains("value=\"+47 412 34 567\"", await Tekst(klient, "/konto?endre=telefon"));

        // Tomt felt fjerner nummeret, og skjemaet star apent igjen.
        Assert.True(Skjemaklient.GikkGjennom(await LagreTelefon(klient, "")));
        var tom = await Tekst(klient, "/konto");
        Assert.DoesNotContain("412 34 567", tom);
        Assert.Contains("name=\"Telefon.Nummer\"", tom);
    }

    [Fact]
    public async Task Ugyldig_telefonnummer_avvises()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);

        var svar = await LagreTelefon(klient, "ring meg");

        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        Assert.Contains("Telefonnummeret kan bare inneholde sifre, mellomrom og +.",
            WebUtility.HtmlDecode(await svar.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task Telefonnummeret_er_med_i_dataeksporten()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        await LagreTelefon(klient, "41234567");

        var json = await (await klient.Hent("/konto/data")).Content.ReadAsStringAsync();

        Assert.Contains("\"telefon\": \"41234567\"", json);
    }

    // --- Plakaten --------------------------------------------------------

    [Fact]
    public async Task Plakaten_har_kjennetegn_chip_og_telefon()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient, "Luna");
        await LagreTelefon(klient, "412 34 567");

        await klient.Post($"/dyr/{dyrId}/rediger", new Dictionary<string, string>
        {
            ["Navn"] = "Luna",
            ["Art"] = ((int)Art.Hund).ToString(),
            ["Kjonn"] = ((int)Kjonn.Tispe).ToString(),
            ["ChipNr"] = "578097811234567",
            ["Farge"] = "Gul, korthåret",
            ["Kjennetegn"] = "Arr på venstre øre",
            ["ForplanAktiv"] = "true"
        });

        // Knappen star pa informasjonssiden.
        Assert.Contains($"href=\"/informasjon/savnet/{dyrId}\"", await Tekst(klient, "/informasjon"));

        var plakat = await Tekst(klient, $"/informasjon/savnet/{dyrId}");

        Assert.Contains("SAVNET", plakat);
        Assert.Contains("Luna", plakat);
        Assert.Contains("Gul, korthåret", plakat);
        Assert.Contains("Arr på venstre øre", plakat);
        Assert.Contains("Chip 578097811234567", plakat);
        Assert.Contains("412 34 567", plakat);

        // Utskriftsdialogen apnes med knappen, ikke av seg selv, og det finnes
        // ikke noe utvalg a endre.
        Assert.Contains("Skriv ut / lagre som PDF", plakat);
        Assert.DoesNotContain("Endre utvalg", plakat);
    }

    [Fact]
    public async Task Uten_telefonnummer_far_brukeren_beskjed_om_hvor_det_legges_inn()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var plakat = await Tekst(klient, $"/informasjon/savnet/{dyrId}");

        Assert.Contains("Du har ikke lagt inn telefonnummer.", plakat);
        Assert.Contains("href=\"/konto\"", plakat);
    }

    [Fact]
    public async Task Plakaten_er_ikke_med_i_den_vanlige_utskriften()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        await Testoppsett.NyttDyr(klient);

        Assert.DoesNotContain("SAVNET", await Tekst(klient, "/informasjon/utskrift"));
    }

    /// <summary>
    /// Plakatens stilark har @page med marg null. Lastet pa den vanlige
    /// utskriften ville det tatt bort margen der - og @page kan ikke
    /// avgrenses til en klasse, bare til hvilke sider som laster fila.
    /// </summary>
    [Fact]
    public async Task Plakatens_stilark_lastes_bare_pa_plakaten()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        Assert.Contains("/css/plakat.css", await Tekst(klient, $"/informasjon/savnet/{dyrId}"));
        Assert.DoesNotContain("/css/plakat.css", await Tekst(klient, "/informasjon/utskrift"));
    }

    [Fact]
    public async Task En_annen_husstands_dyr_gir_404()
    {
        var eier = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(eier);

        var fremmed = await Testoppsett.InnloggetKlient(_app);

        Assert.Equal(HttpStatusCode.NotFound,
            (await fremmed.Hent($"/informasjon/savnet/{dyrId}")).StatusCode);
    }
}
