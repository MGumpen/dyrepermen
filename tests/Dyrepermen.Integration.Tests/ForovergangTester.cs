using System.Net;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Overgangsplanen: rafor etter vekt, torrfor etter alder, og en andel som
/// flyttes for hand gjennom overgangen. Se ADR 0012.
///
/// Testene bruker en fodselsdato langt over siste rad i tabellen, sa
/// resultatet ikke henger pa hvilken ukedag suiten kjorer.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class ForovergangTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public ForovergangTester(DatabaseFixture fixture) => _fixture = fixture;

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
    /// Standardencoderen koder alt utenfor ASCII, sa "rafor" med a-med-ring
    /// star i kilden som en entitet. En assertion pa norsk tekst feiler da
    /// pa en side som ser helt riktig ut i nettleseren.
    /// </summary>
    private static async Task<string> Side(Skjemaklient klient, string sti)
        => WebUtility.HtmlDecode(
            await (await klient.Hent(sti)).Content.ReadAsStringAsync());

    private static async Task<int> Valp(
        Skjemaklient klient, DateOnly? fodselsdato)
    {
        var dyrId = await Testoppsett.NyttDyr(klient);

        var felter = new Dictionary<string, string>
        {
            ["Navn"] = "Luna",
            ["Art"] = ((int)Art.Hund).ToString(),
            ["Kjonn"] = ((int)Kjonn.Tispe).ToString(),
            ["ForingsloggAktiv"] = "true",
            ["ForplanAktiv"] = "true"
        };

        if (fodselsdato is { } fodt)
        {
            felter["Fodselsdato"] = fodt.ToString("yyyy-MM-dd");
        }

        var svar = await klient.Post(
            $"/dyr/{dyrId}/rediger", felter, tokenFra: $"/dyr/{dyrId}/rediger");

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Dyret ble ikke oppdatert: {await Skjemaklient.Feilmeldinger(svar)}");

        return dyrId;
    }

    private static async Task LeggInnVekt(Skjemaklient klient, int dyrId, string kilo)
    {
        var svar = await klient.Post(
            $"/dyr/{dyrId}/vekt",
            new Dictionary<string, string>
            {
                // Komma, ikke punktum. Kulturen er fast nb-NO.
                ["Kilo"] = kilo,
                ["Dato"] = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd")
            },
            tokenFra: $"/dyr/{dyrId}/vekt");

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Vekten ble ikke lagret: {await Skjemaklient.Feilmeldinger(svar)}");
    }

    private static async Task<HttpResponseMessage> Overgangsplan(
        Skjemaklient klient, int dyrId, string prosent, int vektdelAndel)
        => await klient.Post(
            $"/dyr/{dyrId}/forplan",
            new Dictionary<string, string>
            {
                ["Metode"] = ((int)Formetode.Tabell).ToString(),
                ["BlandToFor"] = "true",
                ["Prosent"] = prosent,
                ["VektdelAndel"] = vektdelAndel.ToString(),
                ["AntallMaltider"] = "2",
                ["Fornavn"] = "Råfôr valp",
                ["FornavnAlder"] = "Tørrfôr valp",
                ["Trinn[0].AlderMnd"] = "3",
                ["Trinn[0].GramPerDag"] = "160",
                ["Trinn[1].AlderMnd"] = "4",
                ["Trinn[1].GramPerDag"] = "180"
            },
            tokenFra: $"/dyr/{dyrId}/forplan");

    /// <summary>Langt forbi siste rad, sa tabellen gir 180 g uansett dag.</summary>
    private static DateOnly Voksen => DateOnly
        .FromDateTime(DateTime.UtcNow)
        .AddDays(-200);

    /// <summary>
    /// Hovedtesten. 8,20 kg x 5 % = 410 g rafor, 70 % av det er 287 g.
    /// Tabellen gir 180 g torrfor, 30 % av det er 54 g. Sum 341 g.
    ///
    /// Summen er IKKE 410 g delt 70/30 - hver fortype skaleres for seg.
    /// </summary>
    [Fact]
    public async Task Overgangsplan_viser_begge_fortypene_og_regnestykket()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Valp(klient, Voksen);
        await LeggInnVekt(klient, dyrId, "8,2");

        var lagret = await Overgangsplan(klient, dyrId, "5,0", 70);
        Assert.True(
            Skjemaklient.GikkGjennom(lagret),
            $"Planen ble ikke lagret: {await Skjemaklient.Feilmeldinger(lagret)}");

        var html = await Side(klient, $"/dyr/{dyrId}/forplan");

        // Dagsmengden og begge delene.
        Assert.Contains("341 g", html);
        Assert.Contains("287 g", html);
        Assert.Contains("54 g", html);

        // Regelen, ikke bare tallet.
        Assert.Contains("70 % etter vekt", html);
        Assert.Contains("30 % etter alder", html);

        // Begge regnestykkene, sa brukeren kan regne etter selv.
        Assert.Contains("8,20 kg", html);
        Assert.Contains("410 g", html);
        Assert.Contains("180 g", html);

        // Tabellen skal sta der i sin helhet, ikke bare dagens oppslag.
        Assert.Contains("3 mnd 160 g", html);
        Assert.Contains("4 mnd 180 g", html);
    }

    /// <summary>
    /// Maltidene fordeles per fortype. Uten det matte den som veier opp
    /// gjore delingen i hodet ved hvert eneste maltid.
    /// </summary>
    [Fact]
    public async Task Maltidene_deles_per_fortype()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Valp(klient, Voksen);
        await LeggInnVekt(klient, dyrId, "8,2");
        await Overgangsplan(klient, dyrId, "5,0", 70);

        var html = await Side(klient, $"/dyr/{dyrId}/forplan");

        // Kolonnene baerer forets navn nar det finnes.
        Assert.Contains("Råfôr valp", html);
        Assert.Contains("Tørrfôr valp", html);

        // 287 g pa to maltider blir 144 + 143, 54 g blir 27 + 27.
        Assert.Contains("144 g", html);
        Assert.Contains("143 g", html);
        Assert.Contains("27 g", html);
    }

    /// <summary>
    /// Samme regel som for vekten: uten grunnlag skal siden si fra, ikke
    /// vise 0 gram. Torrforet slas opp pa alder, sa uten fodselsdato finnes
    /// det ingenting a sla opp i.
    /// </summary>
    [Fact]
    public async Task Uten_fodselsdato_sier_siden_fra_i_stedet_for_a_vise_null()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Valp(klient, fodselsdato: null);
        await LeggInnVekt(klient, dyrId, "8,2");
        await Overgangsplan(klient, dyrId, "5,0", 70);

        var html = await Side(klient, $"/dyr/{dyrId}/forplan");

        Assert.Contains("Fyll inn fødselsdatoen", html);
        Assert.DoesNotContain("0 g</span>", html);
    }

    /// <summary>
    /// Andelen er det ene tallet som flyttes gjennom overgangen. Star ikke
    /// resten av skjemaet ferdig utfylt, ma hele torrfortabellen tastes inn
    /// pa nytt hver uke - og da blir funksjonen ubrukelig.
    /// </summary>
    [Fact]
    public async Task Skjemaet_er_forhandsutfylt_fra_den_aktive_planen()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Valp(klient, Voksen);
        await LeggInnVekt(klient, dyrId, "8,2");
        await Overgangsplan(klient, dyrId, "5,0", 70);

        var html = await Side(klient, $"/dyr/{dyrId}/forplan");

        Assert.Contains("value=\"70\"", html);
        Assert.Contains("value=\"160\"", html);
        Assert.Contains("value=\"180\"", html);
    }

    /// <summary>
    /// En halvt utfylt rad er en skrivefeil, ikke en tom rad. Ignorerte vi
    /// den, satt brukeren igjen med en tabell som mangler et trinn hun trodde
    /// hun hadde lagt inn.
    /// </summary>
    [Fact]
    public async Task Halv_rad_i_tabellen_avvises_med_en_forstaelig_melding()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Valp(klient, Voksen);
        await LeggInnVekt(klient, dyrId, "8,2");

        var svar = await klient.Post(
            $"/dyr/{dyrId}/forplan",
            new Dictionary<string, string>
            {
                ["Metode"] = ((int)Formetode.Tabell).ToString(),
                ["BlandToFor"] = "true",
                ["Prosent"] = "5,0",
                ["VektdelAndel"] = "70",
                ["AntallMaltider"] = "2",
                ["Trinn[0].AlderMnd"] = "3"
            },
            tokenFra: $"/dyr/{dyrId}/forplan");

        Assert.False(Skjemaklient.GikkGjennom(svar));

        var html = WebUtility.HtmlDecode(await svar.Content.ReadAsStringAsync());
        Assert.Contains("både alder og mengde", html);
    }

    /// <summary>
    /// Knappen som apner den avanserte planen. Uten javascript star
    /// metodevelgeren synlig og gjor samme jobb, sa begge skal finnes i
    /// markupen.
    /// </summary>
    [Fact]
    public async Task Forplansiden_tilbyr_avansert_plan()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Valp(klient, Voksen);

        var html = await Side(klient, $"/dyr/{dyrId}/forplan");

        Assert.Contains("Avansert", html);
        Assert.Contains("Tabell fra fôrposen", html);
    }

    /// <summary>
    /// Den viktigste for daglig bruk: oversikten skal vise hvor mye av HVERT
    /// for som skal i, ikke bare totalen. Den som star ved skalen skal ikke
    /// matte apne forplanen for a dele opp maltidet.
    ///
    /// 287 g rafor og 54 g torrfor pa to maltider gir 144 g og 27 g.
    /// </summary>
    [Fact]
    public async Task Oversikten_viser_blandingsforholdet_per_maltid()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Valp(klient, Voksen);
        await LeggInnVekt(klient, dyrId, "8,2");
        await Overgangsplan(klient, dyrId, "5,0", 70);

        var html = await Side(klient, "/");

        // Porsjonen, og oppdelingen av den.
        Assert.Contains("171 g", html);
        Assert.Contains("144 g", html);
        Assert.Contains("27 g", html);

        // Med fornavnene, sa det er til a handle og veie etter.
        Assert.Contains("Råfôr valp", html);
        Assert.Contains("Tørrfôr valp", html);
    }

    /// <summary>
    /// En plan uten blanding skal ikke fa en tom delingslinje.
    /// </summary>
    [Fact]
    public async Task Oversikten_deler_ikke_opp_en_vanlig_plan()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Valp(klient, Voksen);
        await Testoppsett.ForplanIGram(klient, dyrId, 400, 2);

        var html = await Side(klient, "/");

        Assert.Contains("200 g", html);
        Assert.DoesNotContain("Av det", html);
    }

    /// <summary>
    /// Foret skrives inn en gang. Neste plan foreslar det.
    /// </summary>
    [Fact]
    public async Task Tidligere_fornavn_foreslas_i_skjemaet()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Valp(klient, Voksen);
        await LeggInnVekt(klient, dyrId, "8,2");
        await Overgangsplan(klient, dyrId, "5,0", 70);

        // Et helt annet dyr i samme husstand: forslagene er husstandens,
        // ikke dyrets.
        var andreId = await Testoppsett.NyttDyr(klient, "Milo");

        var html = await Side(klient, $"/dyr/{andreId}/forplan");

        Assert.Contains("<datalist id=\"fornavnforslag\">", html);
        Assert.Contains("Råfôr valp", html);
        Assert.Contains("Tørrfôr valp", html);
    }

    // ----- Ren tabellplan, uten innblanding ---------------------------

    /// <summary>
    /// Den vanligste bruken: skriv av forposen, ferdig. Ingen vekt, ingen
    /// prosentsats, ingen blanding - bare alderen.
    /// </summary>
    private static async Task<HttpResponseMessage> Tabellplan(
        Skjemaklient klient, int dyrId)
        => await klient.Post(
            $"/dyr/{dyrId}/forplan",
            new Dictionary<string, string>
            {
                ["Metode"] = ((int)Formetode.Tabell).ToString(),
                ["AntallMaltider"] = "2",
                ["FornavnAlder"] = "Valpefôr",
                ["Trinn[0].AlderMnd"] = "3",
                ["Trinn[0].GramPerDag"] = "160",
                ["Trinn[1].AlderMnd"] = "4",
                ["Trinn[1].GramPerDag"] = "180"
            },
            tokenFra: $"/dyr/{dyrId}/forplan");

    [Fact]
    public async Task Tabellplan_uten_vekt_gir_mengde_etter_alder()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Valp(klient, Voksen);

        var lagret = await Tabellplan(klient, dyrId);
        Assert.True(
            Skjemaktig(lagret),
            $"Planen ble ikke lagret: {await Skjemaklient.Feilmeldinger(lagret)}");

        var html = await Side(klient, $"/dyr/{dyrId}/forplan");

        // 180 g fra tabellen, delt pa to maltider. Ingen vekt registrert.
        Assert.Contains("180 g", html);
        Assert.Contains("90 g", html);
        Assert.Contains("Følger alderen", html);
        Assert.Contains("Tabellen på fôrposen, etter alder", html);

        // Ingen blanding a vise. Feltene for det andre foret ligger i
        // skjemaet - skjult til bryteren slas pa - sa assertionen ma treffe
        // RESULTATET. Sum-kolonnen finnes bare i maltidstabellen til en
        // blandet plan.
        Assert.DoesNotContain("Sum</th>", html);
        Assert.DoesNotContain("Registrer en vekt", html);
    }

    /// <summary>
    /// Er hunden eldre enn siste rad, holdes mengden - men siden sier fra,
    /// sa et flatt tall ikke ser ut som en beregning.
    /// </summary>
    [Fact]
    public async Task Alder_utenfor_tabellen_gir_en_merknad()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Valp(klient, Voksen);
        await Tabellplan(klient, dyrId);

        var html = await Side(klient, $"/dyr/{dyrId}/forplan");

        Assert.Contains("utenfor tabellen", html);
        Assert.Contains("180 g", html);
    }

    /// <summary>
    /// En tabellplan uten en eneste rad er ingen plan. Da ma skjemaet si
    /// fra, ikke lagre noe som alltid gir null gram.
    /// </summary>
    [Fact]
    public async Task Tom_tabell_avvises()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Valp(klient, Voksen);

        var svar = await klient.Post(
            $"/dyr/{dyrId}/forplan",
            new Dictionary<string, string>
            {
                ["Metode"] = ((int)Formetode.Tabell).ToString(),
                ["AntallMaltider"] = "2"
            },
            tokenFra: $"/dyr/{dyrId}/forplan");

        Assert.False(Skjemaktig(svar));

        var html = WebUtility.HtmlDecode(await svar.Content.ReadAsStringAsync());
        Assert.Contains("minst én rad", html);
    }

    /// <summary>
    /// Oversikten skal ikke dele opp en plan som ikke blander. "0 g etter
    /// vekt" er stoy, ikke opplysning.
    /// </summary>
    [Fact]
    public async Task Oversikten_deler_ikke_opp_en_ren_tabellplan()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Valp(klient, Voksen);
        await Tabellplan(klient, dyrId);

        var html = await Side(klient, "/");

        Assert.Contains("90 g", html);
        Assert.DoesNotContain("Av det", html);
    }

    private static bool Skjemaktig(HttpResponseMessage svar)
        => Skjemaklient.GikkGjennom(svar);
}
