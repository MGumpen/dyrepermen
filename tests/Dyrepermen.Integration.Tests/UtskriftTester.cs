using System.Net;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Utskriftssiden samler alt om alle dyr. Den skal ta med det som er
/// relevant a ha pa papir, og utelate det som ikke er det.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class UtskriftTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public UtskriftTester(DatabaseFixture fixture) => _fixture = fixture;

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

    [Fact]
    public async Task Alle_dyr_blir_med_uten_at_noe_velges()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        await Testoppsett.NyttDyr(klient, "Luna");
        await Testoppsett.NyttDyr(klient, "Tiger");
        await Testoppsett.NyttDyr(klient, "Pelle");

        var svar = await klient.Hent("/informasjon/utskrift");
        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);

        var html = await svar.Content.ReadAsStringAsync();

        Assert.Contains("Luna", html);
        Assert.Contains("Tiger", html);
        Assert.Contains("Pelle", html);

        // Ett avsnitt per dyr - det er de som far hver sin side.
        Assert.Equal(3, Antall(html, "utskrift-dyr"));
    }

    [Fact]
    public async Task Vekt_forplan_og_forsikring_er_med()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient, "Luna");
        await Testoppsett.ForplanIGram(klient, dyrId, 400, 2);

        await klient.Post($"/dyr/{dyrId}/vekt", new Dictionary<string, string>
        {
            ["Kilo"] = "12,5",
            ["Dato"] = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd")
        });

        await klient.Post("/forsikring", new Dictionary<string, string>
        {
            ["DyrId"] = dyrId.ToString(),
            ["Selskap"] = "Gjensidige",
            ["ArspremieKr"] = "4200",
            ["ForsikringsbelopKr"] = "60000",
            ["EgenandelFastKr"] = "1500",
            ["EgenandelVariabelProsent"] = "20"
        });

        var html = await (await klient.Hent("/informasjon/utskrift"))
            .Content.ReadAsStringAsync();

        Assert.Contains("12,50 kg", html);
        Assert.Contains("400 g per dag", html);
        Assert.Contains("Gjensidige", html);
        Assert.Contains("Forsikring", html);
    }

    [Fact]
    public async Task Veterinaer_handleliste_og_forfall_er_utelatt()
    {
        // Disse ble bevisst holdt utenfor. Et ark er et oyeblikksbilde: "hva
        // forfaller de neste 14 dagene" er utdatert dagen etter, og
        // handlelisten hoerer hjemme i butikken.
        var klient = await Testoppsett.InnloggetKlient(_app);
        await Testoppsett.NyttDyr(klient, "Luna");

        await klient.Post("/veterinar", new Dictionary<string, string>
        {
            ["Navn"] = "Vestkanten Dyreklinikk",
            ["Type"] = "0",
            ["Telefon"] = "55 12 34 56"
        });

        await klient.Post("/handleliste", new Dictionary<string, string>
        {
            ["tekst"] = "Torrfor",
            ["antall"] = "2"
        });

        var html = await (await klient.Hent("/informasjon/utskrift"))
            .Content.ReadAsStringAsync();

        Assert.DoesNotContain("Vestkanten", html);
        Assert.DoesNotContain("Torrfor", html);
        Assert.DoesNotContain("Forfaller", html);
    }

    [Fact]
    public async Task Notater_folger_dyret_sitt()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient, "Luna");

        await klient.Post("/informasjon", new Dictionary<string, string>
        {
            ["Ny.Tittel"] = "Forvaner",
            ["Ny.Tekst"] = "Spiser ikke for 07",
            ["Ny.DyrId"] = dyrId.ToString()
        });

        await klient.Post("/informasjon", new Dictionary<string, string>
        {
            ["Ny.Tittel"] = "Portkode",
            ["Ny.Tekst"] = "1234"
        });

        var html = await (await klient.Hent("/informasjon/utskrift"))
            .Content.ReadAsStringAsync();

        Assert.Contains("Forvaner", html);
        // Notat uten dyr havner i sin egen bolk til slutt.
        Assert.Contains("Portkode", html);
        Assert.Contains("Felles", html);
    }

    [Fact]
    public async Task Knappen_star_pa_informasjonssiden()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);

        var html = await (await klient.Hent("/informasjon"))
            .Content.ReadAsStringAsync();

        Assert.Contains("/informasjon/utskrift", html);
        Assert.Contains("Lagre informasjon som PDF", html);
    }

    [Fact]
    public async Task Utskriften_viser_ikke_en_annen_husstands_dyr()
    {
        // Query-filtrene gjor jobben, men utskriften er en ny inngang til
        // dataene - og en ny inngang er et nytt sted filteret kan mangle.
        var minKlient = await Testoppsett.InnloggetKlient(_app);
        await Testoppsett.NyttDyr(minKlient, "MittDyr");

        var annenKlient = await Testoppsett.InnloggetKlient(_app);
        await Testoppsett.NyttDyr(annenKlient, "AnnetDyr");

        var html = await (await annenKlient.Hent("/informasjon/utskrift"))
            .Content.ReadAsStringAsync();

        Assert.Contains("AnnetDyr", html);
        Assert.DoesNotContain("MittDyr", html);
    }

    private static int Antall(string tekst, string bit)
    {
        var n = 0;
        var i = tekst.IndexOf(bit, StringComparison.Ordinal);
        while (i >= 0)
        {
            n++;
            i = tekst.IndexOf(bit, i + bit.Length, StringComparison.Ordinal);
        }
        return n;
    }
}
