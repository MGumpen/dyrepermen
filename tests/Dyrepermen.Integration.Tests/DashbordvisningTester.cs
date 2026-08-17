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
    /// Oppretter et dyr med fast fôrmengde i gram, og returnerer id-en.
    /// Gram og ikke prosent: da er porsjonen det samme tallet uansett om
    /// dyret har en vekt registrert.
    /// </summary>
    private static async Task<int> DyrMedForplan(
        Skjemaklient klient, int gramPerDag, int antallMaltider)
    {
        var dyrId = await Testoppsett.NyttDyr(klient);
        await Testoppsett.ForplanIGram(klient, dyrId, gramPerDag, antallMaltider);
        return dyrId;
    }

    [Fact]
    public async Task Uten_foringslogg_viser_kortet_porsjon_og_antall_maltider()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);

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
        var klient = await Testoppsett.InnloggetKlient(_app);

        var dyrId = await DyrMedForplan(klient, gramPerDag: 159, antallMaltider: 3);

        // Bryteren star per dyr, pa redigeringsskjemaet.
        await Testoppsett.SlaPaForingslogg(klient, dyrId);

        var svar = await klient.Hent("/");
        var html = await svar.Content.ReadAsStringAsync();

        // Na betyr telleren noe, og da skal den staa der.
        Assert.Contains("Neste måltid", html);
        Assert.Contains("nr. 1 av 3", html);
        Assert.DoesNotContain("3 måltider om dagen", html);
    }
}
