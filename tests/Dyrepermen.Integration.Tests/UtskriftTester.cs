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

        // Knappen gar til valgsiden, ikke rett til utskriften.
        Assert.Contains("/informasjon/utskrift/velg", html);
        Assert.Contains("Lagre som PDF", html);
    }

    // --- Utvalg ----------------------------------------------------------

    [Fact]
    public async Task Valgsiden_har_alle_dyr_og_alle_deler_krysset_av()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var luna = await Testoppsett.NyttDyr(klient, "Luna");
        var tiger = await Testoppsett.NyttDyr(klient, "Tiger");

        var svar = await klient.Hent("/informasjon/utskrift/velg");
        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        var html = await svar.Content.ReadAsStringAsync();

        Assert.Matches($"""name="dyr" value="{luna}"[^>]*checked""", html);
        Assert.Matches($"""name="dyr" value="{tiger}"[^>]*checked""", html);

        foreach (var del in new[]
        {
            "OmDyret", "Forplan", "Vekt", "Behandlinger", "Medisiner",
            "Forsikring", "Notater", "FellesNotater"
        })
        {
            Assert.Matches($"""name="del" value="{del}"[^>]*checked""", html);
        }

        Assert.Contains("Lag PDF", html);
    }

    [Fact]
    public async Task Bare_de_valgte_dyrene_blir_med()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var luna = await Testoppsett.NyttDyr(klient, "Luna");
        await Testoppsett.NyttDyr(klient, "Tiger");

        var html = await Side(klient, $"valgt=true&dyr={luna}&del=OmDyret");

        Assert.Contains("Luna", html);
        Assert.DoesNotContain("Tiger", html);
        Assert.Equal(1, Antall(html, "utskrift-dyr"));
    }

    [Fact]
    public async Task Bare_de_valgte_delene_blir_med()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient, "Luna");

        await klient.Post($"/dyr/{dyrId}/vekt", new Dictionary<string, string>
        {
            ["Kilo"] = "12,5",
            ["Dato"] = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd")
        });
        await klient.Post($"/dyr/{dyrId}/behandling", new Dictionary<string, string>
        {
            ["Type"] = "Ormekur",
            ["Preparat"] = "Milbemax",
            ["Dato"] = "2026-08-01"
        });

        var html = await Side(klient, $"valgt=true&dyr={dyrId}&del=Behandlinger");

        Assert.Contains("<h3>Behandlinger</h3>", html);
        Assert.Contains("Milbemax", html);
        Assert.DoesNotContain("<h3>Vekt</h3>", html);
        Assert.DoesNotContain("12,50 kg", html);
        Assert.DoesNotContain("<h3>Om dyret</h3>", html);
    }

    [Fact]
    public async Task Uten_deler_star_valgsiden_igjen_med_en_feilmelding()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient, "Luna");

        var svar = await klient.Hent($"/informasjon/utskrift?valgt=true&dyr={dyrId}");
        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        var html = await svar.Content.ReadAsStringAsync();

        // Dynamisk tekst HTML-kodes: "én" star som "&#xE9;n" i kilden.
        Assert.Contains("Velg minst én del som skal med.", WebUtility.HtmlDecode(html));
        // Dyret er fortsatt krysset av - valget skal ikke ga tapt.
        Assert.Matches($"""name="dyr" value="{dyrId}"[^>]*checked""", html);
    }

    [Fact]
    public async Task Uten_dyr_star_valgsiden_igjen_med_en_feilmelding()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        await Testoppsett.NyttDyr(klient, "Luna");

        var html = await (await klient.Hent("/informasjon/utskrift?valgt=true&del=Vekt"))
            .Content.ReadAsStringAsync();

        Assert.Contains("Velg minst ett dyr.", html);
    }

    [Fact]
    public async Task En_annen_husstands_dyr_kan_ikke_velges_inn()
    {
        var minKlient = await Testoppsett.InnloggetKlient(_app);
        var mittDyr = await Testoppsett.NyttDyr(minKlient, "MittDyr");

        var annenKlient = await Testoppsett.InnloggetKlient(_app);
        var annetDyr = await Testoppsett.NyttDyr(annenKlient, "AnnetDyr");

        var html = await Side(
            annenKlient, $"valgt=true&dyr={mittDyr}&dyr={annetDyr}&del=OmDyret");

        Assert.Contains("AnnetDyr", html);
        Assert.DoesNotContain("MittDyr", html);
    }

    /// <summary>
    /// Utskriften er en forhandsvisning. Dialogen apnes med knappen, ikke av
    /// seg selv, og utvalget kan endres derfra.
    /// </summary>
    [Fact]
    public async Task Utskriften_apner_ikke_dialogen_av_seg_selv()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient, "Luna");

        var html = await Side(klient, $"valgt=true&dyr={dyrId}&del=Vekt");

        Assert.DoesNotContain("addEventListener('load'", html);
        Assert.Contains("onclick=\"window.print()\"", html);
        Assert.Contains("Skriv ut / lagre som PDF", html);
        Assert.Contains(
            $"/informasjon/utskrift/velg?valgt=true&amp;dyr={dyrId}&amp;del=Vekt", html);
    }

    /// <summary>
    /// En ormekur som er fulgt opp av en nyere, skal ikke sende leseren til
    /// dyrlegen pa en dato som ikke gjelder lenger. Se ADR 0016.
    /// </summary>
    [Fact]
    public async Task Fulgt_opp_behandling_viser_ikke_neste_dato()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient, "Luna");

        foreach (var (dato, neste) in new[]
        {
            ("2026-02-01", "2026-05-01"),
            ("2026-05-03", "2026-08-03")
        })
        {
            await klient.Post($"/dyr/{dyrId}/behandling", new Dictionary<string, string>
            {
                ["Type"] = "Ormekur",
                ["Preparat"] = "Milbemax",
                ["Dato"] = dato,
                ["NesteDato"] = neste
            });
        }

        var html = await Side(klient, $"valgt=true&dyr={dyrId}&del=Behandlinger");

        Assert.Contains("3. august 2026", html);
        Assert.DoesNotContain("1. mai 2026", html);
    }

    private static async Task<string> Side(Skjemaklient klient, string sporring)
    {
        var svar = await klient.Hent($"/informasjon/utskrift?{sporring}");
        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        return await svar.Content.ReadAsStringAsync();
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
