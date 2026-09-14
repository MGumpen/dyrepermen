using System.Net;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Kontaktskjemaet over HTTP, mot den ekte oppstarten.
///
/// Selve utsendingen testes ikke her. Appfabrikk setter SMTP-verten tom, sa
/// ingen test sender ekte e-post - utsendingen verifiseres manuelt, jf. plan
/// kapittel 17. Det testene viser, er at skjemaet validerer, og at det sier
/// fra nar e-posten ikke er satt opp i stedet for a late som den gikk.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class KontaktTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public KontaktTester(DatabaseFixture fixture) => _fixture = fixture;

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

    private static Dictionary<string, string> Skjema(string melding, string type = "Onske")
        => new() { ["Type"] = type, ["Melding"] = melding };

    [Fact]
    public async Task Kontaktsiden_vises_for_innlogget_bruker()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);

        var svar = await klient.Hent("/kontakt");

        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        Assert.Contains("Send melding", await svar.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Menyen_lenker_til_kontaktsiden()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);

        var html = await (await klient.Hent("/")).Content.ReadAsStringAsync();

        Assert.Contains("href=\"/kontakt\"", html);
    }

    [Fact]
    public async Task Tom_melding_avvises()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);

        var svar = await klient.Post("/kontakt", Skjema(""));

        Assert.False(Skjemaklient.GikkGjennom(svar));
        Assert.Contains("Skriv en melding.", await Skjemaklient.Feilmeldinger(svar));
    }

    [Fact]
    public async Task Skjema_uten_type_avvises()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);

        var svar = await klient.Post("/kontakt", Skjema("Hei", type: ""));

        Assert.False(Skjemaklient.GikkGjennom(svar));
        Assert.Contains(
            "Velg hva henvendelsen gjelder.", await Skjemaklient.Feilmeldinger(svar));
    }

    [Fact]
    public async Task For_lang_melding_avvises_med_tusenskille()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);

        var svar = await klient.Post("/kontakt", Skjema(new string('a', 2001)));

        Assert.False(Skjemaklient.GikkGjennom(svar));
        Assert.Contains(
            "Meldingen kan være høyst 2 000 tegn.",
            await Skjemaklient.Feilmeldinger(svar));
    }

    [Fact]
    public async Task Linjeskift_teller_som_ett_tegn()
    {
        // Nettleseren poster linjeskift som \r\n, men teller det som ett tegn
        // i maxlength. For brukeren er dette 2 000 tegn, og skjemaet godtok
        // det - da skal ikke serveren avvise det.
        var klient = await Testoppsett.InnloggetKlient(_app);

        var svar = await klient.Post(
            "/kontakt",
            Skjema(new string('a', 1000) + "\r\n" + new string('a', 999)));

        var feil = await Skjemaklient.Feilmeldinger(svar);
        Assert.DoesNotContain("høyst", feil);
        Assert.Contains("ikke satt opp", feil);
    }

    [Fact]
    public async Task Uten_smtp_sier_skjemaet_fra_og_beholder_teksten()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);

        var svar = await klient.Post("/kontakt", Skjema("Kan dere legge til kaniner"));

        // 200 med skjemaet, ikke 302. En omdirigering ville vist "Takk!" for
        // en melding som aldri ble sendt.
        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        Assert.Contains("ikke satt opp", await Skjemaklient.Feilmeldinger(svar));
        Assert.Contains(
            "Kan dere legge til kaniner", await svar.Content.ReadAsStringAsync());
    }
}
