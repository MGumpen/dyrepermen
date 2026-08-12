using Dyrepermen.Domain.Entities;
using Dyrepermen.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Registrering og innlogging, kjort over HTTP mot den ekte appen.
///
/// Skrevet etter en feil som kostet en bruker tre forsok: skjemaet lovet
/// "minst 10 tegn", mens Identity i tillegg krevde tall, sma og store
/// bokstaver og spesialtegn - fordi standardverdiene aldri var overstyrt.
/// Passordet ble avvist med et krav brukeren aldri hadde sett, og etter
/// hvert ble kontoen opprettet uten at det var tydelig at det hadde skjedd.
///
/// Ingen enhetstest kunne fanget det: hver del var riktig for seg. Feilen
/// la mellom to filer som beskrev samme regel.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class RegistreringTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public RegistreringTester(DatabaseFixture fixture) => _fixture = fixture;

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

    /// <summary>Unik adresse per test, sa rekkefolgen aldri spiller inn.</summary>
    private static string NyEpost() => $"test-{Guid.NewGuid():N}@example.test";

    private Skjemaklient Klient() => new(_app.LagKlient());

    private static Dictionary<string, string> Skjema(string epost, string passord)
        => new()
        {
            ["Epost"] = epost,
            ["Visningsnavn"] = "Testbruker",
            ["Passord"] = passord,
            ["BekreftPassord"] = passord
        };

    private async Task<Bruker?> HentBruker(string epost)
    {
        using var omfang = _app.Services.CreateScope();
        var brukere = omfang.ServiceProvider
            .GetRequiredService<UserManager<Bruker>>();

        return await brukere.Users.SingleOrDefaultAsync(b => b.Email == epost);
    }

    // --- Kontrakten mellom skjema og Identity -------------------------------

    [Fact]
    public void Identity_handhever_noyaktig_de_reglene_skjemaet_lover()
    {
        // DENNE testen er grunnen til at de andre finnes. Sprikte de to,
        // ble et lovlig passord avvist med et krav ingen hadde sett - og den
        // som satt der hadde ingen mate a gjette seg til hva som var galt.
        using var omfang = _app.Services.CreateScope();
        var valg = omfang.ServiceProvider
            .GetRequiredService<IOptions<IdentityOptions>>().Value.Password;

        Assert.Equal(Passordkrav.MinLengde, valg.RequiredLength);

        // Identity sin egen RequireUppercase skal vaere AV: den er
        // ASCII-basert og avviser AE, OE og AA. StorBokstavValidator
        // handhever kravet i stedet, og testen under beviser at den virker.
        Assert.False(valg.RequireUppercase);

        // Identity har disse PA som standard. Star de pa uten at skjemaet
        // sier fra, er vi tilbake i den opprinnelige feilen.
        Assert.False(valg.RequireDigit, "Skjemaet nevner ikke krav om tall.");
        Assert.False(valg.RequireLowercase, "Skjemaet nevner ikke krav om liten bokstav.");
        Assert.False(valg.RequireNonAlphanumeric, "Skjemaet nevner ikke krav om spesialtegn.");
    }

    [Theory]
    [InlineData("Passord")]        // akkurat nok: 7 tegn, en stor
    [InlineData("Abcdef")]         // nayaktig minstelengden
    [InlineData("Kongen123")]      // tall er lov, men ikke pakrevd
    [InlineData("Ærlig1")]         // norsk stor bokstav
    [InlineData("HeltVanligPassordUtenTegn")]
    public async Task Passord_skjemaet_godtar_blir_ogsa_godtatt_av_serveren(
        string passord)
    {
        // Klienten og serveren ma vaere enige. Er klienten strengest, blir
        // brukeren stoppet uten grunn; er serveren strengest, far brukeren en
        // feil den ikke kunne forutse. Det siste er det vi kom fra.
        var epost = NyEpost();
        var svar = await Klient().Post("/registrer", Skjema(epost, passord));

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Passordet '{passord}' ble avvist: {await Skjemaklient.Feilmeldinger(svar)}");

        Assert.NotNull(await HentBruker(epost));
    }

    // --- Avvisning skal IKKE opprette bruker --------------------------------

    [Theory]
    [InlineData("Abc12", "for kort")]
    [InlineData("bareminusker", "ingen stor bokstav")]
    public async Task Avvist_registrering_oppretter_ingen_bruker(
        string passord, string grunn)
    {
        // Kjernen i det som ble meldt: kontoen fantes selv om skjermen viste
        // en feil. Da tror man registreringen feilet, prover pa nytt, og far
        // beskjed om at adressen ikke kan brukes.
        var epost = NyEpost();
        var svar = await Klient().Post("/registrer", Skjema(epost, passord));

        Assert.False(Skjemaklient.GikkGjennom(svar), $"Skulle vaert avvist: {grunn}");
        Assert.Null(await HentBruker(epost));
    }

    [Fact]
    public async Task For_kort_passord_sier_hva_kravet_er()
    {
        var svar = await Klient().Post("/registrer", Skjema(NyEpost(), "Ab1"));

        Assert.Contains("6 tegn", await Skjemaklient.Feilmeldinger(svar));
    }

    [Fact]
    public async Task Passord_uten_stor_bokstav_sier_hva_som_mangler()
    {
        var svar = await Klient().Post("/registrer", Skjema(NyEpost(), "bareminusker"));
        var melding = await Skjemaklient.Feilmeldinger(svar);

        Assert.Contains("stor bokstav", melding);

        // Skal IKKE nevne krav som ikke gjelder lenger. Det var nettopp slike
        // meldinger som gjorde feilen uforstaelig.
        Assert.DoesNotContain("spesialtegn", melding);
        Assert.DoesNotContain("tall", melding);
    }

    [Fact]
    public async Task Samme_adresse_to_ganger_gir_kun_en_bruker()
    {
        var epost = NyEpost();

        Assert.True(Skjemaklient.GikkGjennom(
            await Klient().Post("/registrer", Skjema(epost, "Passord1"))));

        // Ny klient: forste registrering logget inn, og en innlogget bruker
        // som poster registreringsskjemaet er et annet tilfelle.
        var svar = await Klient().Post("/registrer", Skjema(epost, "Passord2"));

        Assert.False(Skjemaklient.GikkGjennom(svar));

        using var omfang = _app.Services.CreateScope();
        var brukere = omfang.ServiceProvider.GetRequiredService<UserManager<Bruker>>();

        Assert.Equal(1, await brukere.Users.CountAsync(b => b.Email == epost));
    }

    // --- Innlogging ---------------------------------------------------------

    [Fact]
    public async Task Nyregistrert_bruker_kan_logge_inn_med_samme_passord()
    {
        // Hele poenget: den som nettopp lagde konto skal komme inn. Feilet
        // dette, ville brukeren sittet ute uten a vite hvorfor.
        var epost = NyEpost();
        const string passord = "Vinter2026";

        Assert.True(Skjemaklient.GikkGjennom(
            await Klient().Post("/registrer", Skjema(epost, passord))));

        var svar = await Klient().Post("/logg-inn", new Dictionary<string, string>
        {
            ["Epost"] = epost,
            ["Passord"] = passord,
            ["HuskMeg"] = "true"
        });

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Innlogging feilet: {await Skjemaklient.Feilmeldinger(svar)}");
    }

    [Fact]
    public async Task Feil_passord_slipper_ikke_inn()
    {
        var epost = NyEpost();
        await Klient().Post("/registrer", Skjema(epost, "Riktig123"));

        var svar = await Klient().Post("/logg-inn", new Dictionary<string, string>
        {
            ["Epost"] = epost,
            ["Passord"] = "Feilpassord9",
            ["HuskMeg"] = "false"
        });

        Assert.False(Skjemaklient.GikkGjennom(svar));
    }

    [Fact]
    public async Task Ukjent_adresse_og_feil_passord_gir_SAMME_melding()
    {
        // Ulike meldinger rekker a rope ut hvem som har konto her. Se plan
        // kapittel 15.
        var epost = NyEpost();
        await Klient().Post("/registrer", Skjema(epost, "Riktig123"));

        var feilPassord = await Klient().Post("/logg-inn", new Dictionary<string, string>
        {
            ["Epost"] = epost,
            ["Passord"] = "Helt feil99",
            ["HuskMeg"] = "false"
        });

        var ukjent = await Klient().Post("/logg-inn", new Dictionary<string, string>
        {
            ["Epost"] = NyEpost(),
            ["Passord"] = "Helt feil99",
            ["HuskMeg"] = "false"
        });

        Assert.Equal(
            await Skjemaklient.Feilmeldinger(feilPassord),
            await Skjemaklient.Feilmeldinger(ukjent));
    }

    [Fact]
    public async Task Registreringssiden_forteller_de_faktiske_kravene()
    {
        // Star det noe annet her enn det som handheves, er vi tilbake i den
        // opprinnelige feilen - bare med et nytt tall.
        var html = await _app.LagKlient().GetStringAsync("/registrer");

        // Avkodes forst: Razor skriver "en" som &#xE9; i HTML-en.
        Assert.Contains(Passordkrav.Hjelpetekst, System.Net.WebUtility.HtmlDecode(html));
    }

    [Fact]
    public async Task Norske_store_bokstaver_teller_som_store_bokstaver()
    {
        // Identity sin innebygde sjekk er ASCII: c >= A og c <= Z. Den ville
        // avvist "Ørnulf" med beskjed om at det mangler en stor bokstav,
        // mens brukeren ser rett pa en. I en norsk app er det uholdbart.
        foreach (var passord in new[] { "Ærlig1", "Ørnulf", "Åpenbar" })
        {
            var epost = NyEpost();
            var svar = await Klient().Post("/registrer", Skjema(epost, passord));

            Assert.True(
                Skjemaklient.GikkGjennom(svar),
                $"'{passord}' ble avvist: {await Skjemaklient.Feilmeldinger(svar)}");
        }
    }

    [Fact]
    public async Task Innloggingssiden_har_en_synlig_vei_til_registrering()
    {
        // Meldt inn: knappen var en gra smatekstlenke under kortet, og folk
        // fant den ikke.
        var html = await _app.LagKlient().GetStringAsync("/logg-inn");

        Assert.Contains("Opprett ny konto", html);
        Assert.Contains("btn-aksent-omriss", html);
    }

    [Fact]
    public async Task Kjent_adresse_far_beskjed_om_a_logge_inn_i_stedet()
    {
        // Meldingen skal ikke rope ut at adressen finnes - men den som
        // allerede har konto ma fa vite hva han skal gjore, ellers star han
        // og prover nye passord slik det faktisk skjedde.
        var epost = NyEpost();
        await Klient().Post("/registrer", Skjema(epost, "Passord1"));

        var svar = await Klient().Post("/registrer", Skjema(epost, "Passord2"));
        var melding = await Skjemaklient.Feilmeldinger(svar);

        Assert.Contains("logge inn", melding);

        // Skal fortsatt ikke bekrefte at kontoen finnes.
        Assert.DoesNotContain("finnes", melding);
        Assert.DoesNotContain("registrert", melding);
    }
}
