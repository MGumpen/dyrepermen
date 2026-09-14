using System.Net;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Behandlingstypen Annet er den apne kategorien: klipp, bad, eller noe
/// ingen har tenkt pa. Den ma bade komme gjennom CHECK-vilkaret i databasen
/// og vises med riktig navn overalt.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class BehandlingAnnetTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public BehandlingAnnetTester(DatabaseFixture fixture) => _fixture = fixture;

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
    /// Dekoder HTML-entitetene. Standardencoderen koder alt utenfor ASCII,
    /// sa "Flåttmiddel" star i kilden som &#xE5; midt i ordet.
    /// </summary>
    private static async Task<string> Side(Skjemaklient klient, string sti)
        => WebUtility.HtmlDecode(
            await (await klient.Hent(sti)).Content.ReadAsStringAsync());

    private static Dictionary<string, string> Skjema(
        BehandlingType type, string? preparat) => new()
        {
            // Navnet, ikke tallet. Nedtrekkslisten skriver ut enumnavnet.
            ["Type"] = type.ToString(),
            ["Preparat"] = preparat ?? "",
            ["Dato"] = "2026-08-01"
        };

    [Fact]
    public async Task Annet_star_i_nedtrekkslisten()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var html = await Side(klient, $"/dyr/{dyrId}/behandling");

        Assert.Contains("value=\"Annet\"", html);

        // De faste typene skal fortsatt sta der.
        Assert.Contains("value=\"Vaksine\"", html);
        Assert.Contains("Flåttmiddel", html);
    }

    /// <summary>
    /// Selve poenget: en behandling som ikke passer i de fem faste, skal
    /// kunne registreres og leses tilbake med sitt eget navn.
    /// </summary>
    [Fact]
    public async Task Annet_kan_registreres_og_vises_med_beskrivelsen()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var svar = await klient.Post(
            $"/dyr/{dyrId}/behandling",
            Skjema(BehandlingType.Annet, "Klipp hos frisøren"),
            tokenFra: $"/dyr/{dyrId}/behandling");

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Behandlingen ble ikke lagret: {await Skjemaklient.Feilmeldinger(svar)}");

        var html = await Side(klient, $"/dyr/{dyrId}/behandling");

        // Ikke "Behandling", som reserven i de gamle kopiene ville gitt.
        Assert.Contains("Annet – Klipp hos frisøren", html);
    }

    /// <summary>
    /// "Annet" alene er en rad som sier ingenting. Da skal skjemaet be om
    /// beskrivelsen framfor a lagre noe ubrukelig.
    /// </summary>
    [Fact]
    public async Task Annet_uten_beskrivelse_avvises()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var svar = await klient.Post(
            $"/dyr/{dyrId}/behandling",
            Skjema(BehandlingType.Annet, null),
            tokenFra: $"/dyr/{dyrId}/behandling");

        Assert.False(Skjemaklient.GikkGjennom(svar));
        Assert.Contains(
            "Skriv hva behandlingen var.",
            WebUtility.HtmlDecode(await svar.Content.ReadAsStringAsync()));
    }

    /// <summary>
    /// Motprove: de faste typene baerer sitt eget navn, og skal fortsatt
    /// kunne registreres uten preparat.
    /// </summary>
    [Fact]
    public async Task Fast_type_uten_preparat_gar_fortsatt_gjennom()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var svar = await klient.Post(
            $"/dyr/{dyrId}/behandling",
            Skjema(BehandlingType.Kloklipp, null),
            tokenFra: $"/dyr/{dyrId}/behandling");

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Behandlingen ble ikke lagret: {await Skjemaklient.Feilmeldinger(svar)}");

        Assert.Contains("Kloklipp", await Side(klient, $"/dyr/{dyrId}/behandling"));
    }

    /// <summary>
    /// Annet skal ogsa na dashbordet og informasjonssiden med riktig navn -
    /// det var nettopp der de fire kopiene av etikettene kunne sprike.
    /// </summary>
    [Fact]
    public async Task Annet_vises_med_riktig_navn_pa_dashbordet()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var skjema = Skjema(BehandlingType.Annet, "Bad");
        // Neste dato innenfor varselvinduet, sa raden havner pa dashbordet.
        skjema["NesteDato"] = DateOnly.FromDateTime(DateTime.Now).AddDays(3)
            .ToString("yyyy-MM-dd");

        var svar = await klient.Post(
            $"/dyr/{dyrId}/behandling", skjema,
            tokenFra: $"/dyr/{dyrId}/behandling");

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Behandlingen ble ikke lagret: {await Skjemaklient.Feilmeldinger(svar)}");

        Assert.Contains("Annet – Bad", await Side(klient, "/"));
    }
}
