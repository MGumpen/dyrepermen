using System.Net;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Dashbordet hentet over HTTP, som en nettleser gjor det.
///
/// Tjenestetestene beviser at tallene er riktige, men ikke at riktig tall
/// havner pa siden. Feilen som ga opphav til denne klassen var nettopp av den
/// typen: DyrKort hadde alle opplysningene, men visningen valgte feil gren og
/// viste en teller som aldri beveget seg.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class DashbordvisningTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public DashbordvisningTester(DatabaseFixture fixture) => _fixture = fixture;

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
    /// Registrerer en fersk bruker med egen husstand, og returnerer klienten
    /// hennes innlogget.
    ///
    /// Begge stegene trengs. Registreringen oppretter kontoen og logger inn,
    /// men uten invitasjon lander brukeren pa oppsettsiden - dashbordet er
    /// stengt til husstanden finnes, og en test som hopper over steg to far
    /// 302 i stedet for siden den skulle lest.
    /// </summary>
    private async Task<Skjemaklient> InnloggetKlient()
    {
        var klient = new Skjemaklient(_app.LagKlient());

        var registrert = await klient.Post("/registrer", new Dictionary<string, string>
        {
            ["Epost"] = $"dash-{Guid.NewGuid():N}@example.test",
            ["Visningsnavn"] = "Testbruker",
            ["Passord"] = "Passord123",
            ["BekreftPassord"] = "Passord123"
        });

        Assert.True(
            Skjemaklient.GikkGjennom(registrert),
            $"Registreringen feilet: {await Skjemaklient.Feilmeldinger(registrert)}");

        // Skjemaet star pa /husstand/oppsett og postes til /husstand/opprett.
        var opprettet = await klient.Post(
            "/husstand/opprett",
            new Dictionary<string, string> { ["Navn"] = "Testhusstanden" },
            tokenFra: "/husstand/oppsett");

        Assert.True(
            Skjemaklient.GikkGjennom(opprettet),
            $"Husstanden ble ikke opprettet: {await Skjemaklient.Feilmeldinger(opprettet)}");

        return klient;
    }

    /// <summary>
    /// Oppretter et dyr med fast fôrmengde i gram, og returnerer id-en.
    /// Gram og ikke prosent: da er porsjonen det samme tallet uansett om
    /// dyret har en vekt registrert.
    /// </summary>
    private static async Task<int> DyrMedForplan(
        Skjemaklient klient, int gramPerDag, int antallMaltider)
    {
        var lagret = await klient.Post("/dyr/ny", new Dictionary<string, string>
        {
            ["Navn"] = "Luna",
            ["Art"] = ((int)Art.Hund).ToString(),
            ["Kjonn"] = ((int)Kjonn.Tispe).ToString()
        });

        Assert.True(
            Skjemaklient.GikkGjennom(lagret),
            $"Dyret ble ikke lagret: {await Skjemaklient.Feilmeldinger(lagret)}");

        // Id-en star i omdirigeringen: /dyr/{id}.
        var dyrId = int.Parse(
            lagret.Headers.Location!.ToString().Split('/').Last());

        var plan = await klient.Post(
            $"/dyr/{dyrId}/forplan",
            new Dictionary<string, string>
            {
                ["Metode"] = ((int)Formetode.Gram).ToString(),
                ["GramPerDag"] = gramPerDag.ToString(),
                ["AntallMaltider"] = antallMaltider.ToString()
            },
            tokenFra: $"/dyr/{dyrId}/forplan");

        Assert.True(
            Skjemaklient.GikkGjennom(plan),
            $"Fôrplanen ble ikke lagret: {await Skjemaklient.Feilmeldinger(plan)}");

        return dyrId;
    }

    [Fact]
    public async Task Uten_foringslogg_viser_kortet_porsjon_og_antall_maltider()
    {
        var klient = await InnloggetKlient();

        // 159 g pa 3 maltider gir 53 g per maltid. Foringsloggen er av som
        // standard - den er en funksjonsbryter man ma sla PA.
        await DyrMedForplan(klient, gramPerDag: 159, antallMaltider: 3);

        var svar = await klient.Hent("/");
        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);

        var html = await svar.Content.ReadAsStringAsync();

        Assert.Contains("53 g", html);
        Assert.Contains("3 måltider om dagen", html);

        // Kjernen i feilen: telleren sto pa "nr. 1 av 3" hele dagen fordi
        // ingen maltider kunne registreres. Et tall som ser ut som framdrift
        // og aldri beveger seg, er verre enn ingen tall.
        Assert.DoesNotContain("nr. 1 av 3", html);
        Assert.DoesNotContain("Neste måltid", html);
    }

    [Fact]
    public async Task Med_foringslogg_teller_kortet_maltidene()
    {
        var klient = await InnloggetKlient();

        var dyrId = await DyrMedForplan(klient, gramPerDag: 159, antallMaltider: 3);

        // Bryteren star per dyr, pa redigeringsskjemaet.
        var pa = await klient.Post(
            $"/dyr/{dyrId}/rediger",
            new Dictionary<string, string>
            {
                ["Navn"] = "Luna",
                ["Art"] = ((int)Art.Hund).ToString(),
                ["Kjonn"] = ((int)Kjonn.Tispe).ToString(),
                ["ForingsloggAktiv"] = "true",

                // Ma vaere med. Skjemaet poster begge bryterne, og utelates
                // denne binder den til false - da forsvinner porsjonen, og
                // testen ville feilet pa noe helt annet enn det den maler.
                ["ForplanAktiv"] = "true"
            },
            tokenFra: $"/dyr/{dyrId}/rediger");

        Assert.True(
            Skjemaklient.GikkGjennom(pa),
            $"Kunne ikke sla pa foringsloggen: {await Skjemaklient.Feilmeldinger(pa)}");

        var svar = await klient.Hent("/");
        var html = await svar.Content.ReadAsStringAsync();

        // Na betyr telleren noe, og da skal den staa der.
        Assert.Contains("Neste måltid", html);
        Assert.Contains("nr. 1 av 3", html);
        Assert.DoesNotContain("3 måltider om dagen", html);
    }
}
