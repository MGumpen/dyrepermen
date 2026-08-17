using System.Net;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Hver underside av et dyr skal ha en vei tilbake til dyret.
///
/// For hadde ingen av dem en tilbakeknapp. Dyrenavnet lo som undertittel i
/// svak gra - det VAR en lenke, men ingenting ved den sa slik ut, og eneste
/// mate a komme et hakk tilbake pa var a gjette at den var klikkbar.
///
/// Listen under er bevisst eksplisitt. Legges det til en ny underside uten at
/// den fores opp her, sier testen ingenting - men fores den opp uten at siden
/// bruker _Dyrhode, feiler den med en gang.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class DyrhodeTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public DyrhodeTester(DatabaseFixture fixture) => _fixture = fixture;

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

    /// <summary>Undersidene, slik de star i rutene.</summary>
    public static TheoryData<string> Undersider() =>
    [
        "vekt",
        "forplan",
        "behandling",
        "medisin",
        "foring",
        "rediger"
    ];

    [Theory]
    [MemberData(nameof(Undersider))]
    public async Task Undersiden_har_en_vei_tilbake_til_dyret(string underside)
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient, "Luna");

        // Foringssiden ligger bak en funksjonsbryter som er av pa et nytt
        // dyr. Med den av svarer siden 404 - riktig oppforsel, men da finnes
        // det ikke noe hode a se etter.
        if (underside == "foring")
        {
            await Testoppsett.SlaPaForingslogg(klient, dyrId, "Luna");
        }

        var svar = await klient.Hent($"/dyr/{dyrId}/{underside}");
        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);

        var html = await svar.Content.ReadAsStringAsync();

        // Lenken peker pa dyret, ikke pa dyrelisten. Man skal ett hakk
        // tilbake, ikke helt ut.
        Assert.Contains($"href=\"/dyr/{dyrId}\"", html);

        // Og den er merket som en knapp. Uten dette kunne lenken vaert den
        // gamle undertittelen, som pekte samme sted uten at noe ved den sa
        // ut som noe man kunne trykke pa.
        Assert.Contains("Tilbake", html);

        // Navnet star fortsatt pa siden, sa man ser hvilket dyr den gjelder.
        Assert.Contains("Luna", html);
    }

    [Fact]
    public async Task Dyresiden_selv_har_ingen_tilbakeknapp()
    {
        // Motprøven. Uten den ville testen over bestatt selv om knappen lo i
        // layouten og dermed pa hver eneste side i appen - og da beviser den
        // ingenting om undersidene.
        //
        // Dyresiden er ogsa det riktige stedet a male det: det er hit
        // knappen gar, og derfra skal den ikke finnes.
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient, "Luna");

        var svar = await klient.Hent($"/dyr/{dyrId}");
        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);

        Assert.DoesNotContain(
            "Tilbake", await svar.Content.ReadAsStringAsync());
    }
}
