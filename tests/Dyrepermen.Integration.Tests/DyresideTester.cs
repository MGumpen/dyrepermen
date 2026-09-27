using System.Net;
using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Dyrets side viser det som kommer - kommende behandlinger og aktive
/// medisiner - med lenke til historikken og en knapp for a registrere nytt.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class DyresideTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public DyresideTester(DatabaseFixture fixture) => _fixture = fixture;

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

    private static DateOnly Idag => Tidssone.Idag(DateTimeOffset.UtcNow);

    private static Task<HttpResponseMessage> Behandling(
        Skjemaklient klient, int dyrId, string type, string preparat,
        DateOnly dato, DateOnly? neste)
        => klient.Post($"/dyr/{dyrId}/behandling", new Dictionary<string, string>
        {
            ["Type"] = type,
            ["Preparat"] = preparat,
            ["Dato"] = dato.ToString("yyyy-MM-dd"),
            ["NesteDato"] = neste?.ToString("yyyy-MM-dd") ?? ""
        });

    [Fact]
    public async Task Alle_kommende_behandlinger_vises_men_ikke_de_som_er_fulgt_opp()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        // Tre med neste gang. Den eldste ormekuren er fulgt opp av den nyere.
        await Behandling(klient, dyrId, "Ormekur", "Milbemax",
            Idag.AddMonths(-6), Idag.AddMonths(-3));
        await Behandling(klient, dyrId, "Ormekur", "Milbemax",
            Idag.AddMonths(-3), Idag.AddDays(-2));
        await Behandling(klient, dyrId, "Vaksine", "Nobivac KC",
            Idag.AddYears(-1), Idag.AddDays(30));

        var html = WebUtility.HtmlDecode(
            await (await klient.Hent($"/dyr/{dyrId}")).Content.ReadAsStringAsync());

        Assert.Contains("Kommende behandlinger", html);
        Assert.Contains("Ormekur – Milbemax", html);
        Assert.Contains("Vaksine – Nobivac KC", html);

        // Den nyere ormekuren er forfalt, den fulgt opp er ikke med.
        Assert.Contains("forfalt", html);
        Assert.Contains(Norsk.Dato(Idag.AddDays(-2), "d. MMM yyyy"), html);
        Assert.DoesNotContain(Norsk.Dato(Idag.AddMonths(-3), "d. MMM yyyy"), html);

        // Historikken teller alle tre, ogsa den som er fulgt opp.
        Assert.Contains("Se historikk (3)", html);
    }

    [Fact]
    public async Task Knappene_for_a_registrere_peker_pa_skjemaene()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var svar = await klient.Hent($"/dyr/{dyrId}");
        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        var html = WebUtility.HtmlDecode(await svar.Content.ReadAsStringAsync());

        Assert.Contains($"href=\"/dyr/{dyrId}/behandling#skjema\"", html);
        Assert.Contains("Registrer behandling", html);
        Assert.Contains($"href=\"/dyr/{dyrId}/medisin#skjema\"", html);
        Assert.Contains("Registrer medisin", html);

        // Ingenting a vise historikk for ennå.
        Assert.DoesNotContain("Se historikk", html);

        // Malene finnes pa sidene lenkene gar til.
        var medisin = await (await klient.Hent($"/dyr/{dyrId}/medisin")).Content
            .ReadAsStringAsync();
        Assert.Contains("id=\"skjema\"", medisin);
    }
}
