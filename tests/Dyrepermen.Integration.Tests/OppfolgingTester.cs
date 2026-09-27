using System.Net;
using System.Text.RegularExpressions;
using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Services;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Dyrepermen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Paminnelser som folges opp der de star: "gitt" pa en behandling, "gi dose"
/// pa en medisin, "bestill time" pa en kontroll. Og regelen som gjor at en
/// paminnelse forsvinner nar den er fulgt opp - ogsa nar den nye behandlingen
/// ble registrert pa vanlig mate. Se ADR 0016.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed partial class OppfolgingTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public OppfolgingTester(DatabaseFixture fixture) => _fixture = fixture;

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

    /// <summary>Samme dato som tjenestene regner med - norsk, ikke UTC.</summary>
    private static DateOnly Idag => Tidssone.Idag(DateTimeOffset.UtcNow);

    private static async Task<int> NyttDyr(
        DyrepermenDbContext db, int husstand, string navn = "Luna")
    {
        var dyr = new Dyr
        {
            HusstandId = husstand,
            Navn = navn,
            Art = Art.Hund,
            Kjonn = Kjonn.Tispe
        };
        db.Dyr.Add(dyr);
        await db.SaveChangesAsync();
        return dyr.Id;
    }

    private static async Task<int> Behandling(
        DyrepermenDbContext db, int dyrId, BehandlingType type, string? preparat,
        DateOnly dato, DateOnly? neste)
    {
        var rad = new Behandling
        {
            DyrId = dyrId,
            Type = type,
            Preparat = preparat,
            Dato = dato,
            NesteDato = neste
        };
        db.Behandling.Add(rad);
        await db.SaveChangesAsync();
        return rad.Id;
    }

    private static Task<Dashbord> Dashbord(DyrepermenDbContext db, int husstand)
        => new DashbordService(
            db, new HandlelisteService(db,
                new Husstandskontekst { HusstandId = husstand })).Hent(default);

    // --- Gitt ------------------------------------------------------------

    [Fact]
    public async Task Dialogen_viser_hva_som_gis_og_hva_som_sto_sist()
    {
        var h = await _fixture.OpprettHusstand("Gittgrunnlag");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var forrige = await Behandling(db, dyrId, BehandlingType.Ormekur, "Milbemax",
            Idag.AddMonths(-3), Idag);

        var forslag = await new BehandlingService(db).HentGittgrunnlag(dyrId, forrige, default);

        Assert.NotNull(forslag);
        Assert.True(forslag.KanGis);
        Assert.Equal(Idag.AddMonths(-3), forslag.ForrigeDato);
        Assert.Equal(Idag, forslag.ForrigeNeste);
        Assert.Equal("Ormekur – Milbemax", forslag.Beskrivelse);
        Assert.Equal("Luna", forslag.DyreNavn);
    }

    [Fact]
    public async Task Gitt_registrerer_samme_behandling_i_dag_med_valgt_neste_gang()
    {
        var h = await _fixture.OpprettHusstand("Gitt");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        // Gitt for tre maneder siden, med neste gang i dag.
        var forrige = await Behandling(db, dyrId, BehandlingType.Ormekur, "Milbemax",
            Idag.AddMonths(-3), Idag);

        var resultat = await new BehandlingService(db).Gitt(
            dyrId, forrige, Idag.AddMonths(3), default);

        Assert.Equal(Gittstatus.Lagret, resultat.Status);
        Assert.Equal(Idag.AddMonths(3), resultat.NesteDato);
        Assert.Equal("Ormekur – Milbemax", resultat.Beskrivelse);

        var ny = await db.Behandling.AsNoTracking()
            .SingleAsync(b => b.DyrId == dyrId && b.Id != forrige);

        Assert.Equal(BehandlingType.Ormekur, ny.Type);
        Assert.Equal("Milbemax", ny.Preparat);
        Assert.Equal(Idag, ny.Dato);
        Assert.Equal(Idag.AddMonths(3), ny.NesteDato);

        // Den gamle raden er historikk og star urort.
        var gammel = await db.Behandling.AsNoTracking().SingleAsync(b => b.Id == forrige);
        Assert.Equal(Idag, gammel.NesteDato);
    }

    [Fact]
    public async Task Gitt_fjerner_paminnelsen_fra_oversikten()
    {
        var h = await _fixture.OpprettHusstand("Gitt oversikt");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var forrige = await Behandling(db, dyrId, BehandlingType.Ormekur, "Milbemax",
            Idag.AddMonths(-3).AddDays(-5), Idag.AddDays(-5));

        Assert.Single((await Dashbord(db, h)).Forfaller,
            p => p.Kilde == Kilde.Behandling && p.KildeId == forrige);

        await new BehandlingService(db).Gitt(dyrId, forrige, Idag.AddMonths(3), default);

        var etter = await Dashbord(db, h);

        // Neste gang er tre maneder fram, langt utenfor varselvinduet.
        Assert.DoesNotContain(etter.Forfaller, p => p.Kilde == Kilde.Behandling);
        Assert.Equal(Idag.AddMonths(3), etter.Dyr.Single().NesteBehandlingDato);
    }

    /// <summary>
    /// Den opprinnelige feilen: forrige ormekur sto som "forfalt" pa
    /// dashbordet for alltid, fordi ingenting koblet den nye til den gamle.
    /// Regelen gjelder uansett hvordan den nye kom inn.
    /// </summary>
    [Fact]
    public async Task Ny_behandling_registrert_pa_vanlig_mate_folger_opp_den_gamle()
    {
        var h = await _fixture.OpprettHusstand("Vanlig registrering");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        await Behandling(db, dyrId, BehandlingType.Ormekur, "Milbemax",
            Idag.AddMonths(-3), Idag.AddDays(-2));

        // Store og sma bokstaver teller ikke, og den nye trenger ingen neste
        // gang for a folge opp den gamle.
        await new BehandlingService(db).Registrer(new Behandlingsinnhold(
            dyrId, BehandlingType.Ormekur, "MILBEMAX", Idag, null, null), default);

        var dashbord = await Dashbord(db, h);

        Assert.DoesNotContain(dashbord.Forfaller, p => p.Kilde == Kilde.Behandling);
        Assert.Null(dashbord.Dyr.Single().NesteBehandlingDato);
    }

    /// <summary>
    /// Preparatet er en del av noklen. To vaksiner med hver sin syklus skal
    /// ikke skjule hverandre.
    /// </summary>
    [Fact]
    public async Task Et_annet_preparat_folger_ikke_opp_paminnelsen()
    {
        var h = await _fixture.OpprettHusstand("To vaksiner");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var kennelhoste = await Behandling(db, dyrId, BehandlingType.Vaksine,
            "Nobivac KC", Idag.AddYears(-1), Idag.AddDays(5));
        await Behandling(db, dyrId, BehandlingType.Vaksine,
            "Nobivac DHPPi", Idag, Idag.AddYears(3));

        Assert.Single((await Dashbord(db, h)).Forfaller,
            p => p.Kilde == Kilde.Behandling && p.KildeId == kennelhoste);
    }

    [Fact]
    public async Task Gitt_to_ganger_gir_bare_en_ny_behandling()
    {
        var h = await _fixture.OpprettHusstand("Dobbelttrykk");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var tjeneste = new BehandlingService(db);

        var forrige = await Behandling(db, dyrId, BehandlingType.Flatt, "Bravecto",
            Idag.AddMonths(-3), Idag);

        Assert.Equal(Gittstatus.Lagret,
            (await tjeneste.Gitt(dyrId, forrige, null, default)).Status);
        Assert.Equal(Gittstatus.AlleredeFulgtOpp,
            (await tjeneste.Gitt(dyrId, forrige, null, default)).Status);

        Assert.Equal(2, await db.Behandling.CountAsync(b => b.DyrId == dyrId));
    }

    [Fact]
    public async Task Gitt_uten_neste_gang_gir_ingen_ny_paminnelse()
    {
        var h = await _fixture.OpprettHusstand("Uten neste");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var forrige = await Behandling(db, dyrId, BehandlingType.Ormekur, "Milbemax",
            Idag.AddMonths(-3), Idag.AddDays(-1));

        Assert.Equal(Gittstatus.Lagret,
            (await new BehandlingService(db).Gitt(dyrId, forrige, null, default)).Status);

        var dashbord = await Dashbord(db, h);
        Assert.DoesNotContain(dashbord.Forfaller, p => p.Kilde == Kilde.Behandling);
        Assert.Null(dashbord.Dyr.Single().NesteBehandlingDato);
    }

    /// <summary>
    /// Feilen som ble meldt: en behandling registrert i dag, med neste gang i
    /// morgen. "Gitt i dag" ga en ny rad med samme dato og - med et intervall
    /// pa en dag - samme neste gang. Raden sa uendret ut, og knappen sto der
    /// fortsatt. Hvert trykk ga en kopi til.
    /// </summary>
    [Fact]
    public async Task Behandling_gitt_i_dag_kan_ikke_krysses_av_igjen()
    {
        var h = await _fixture.OpprettHusstand("Gitt i dag");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var tjeneste = new BehandlingService(db);

        var idag = await Behandling(db, dyrId, BehandlingType.Ormekur, "Milbemax",
            Idag, Idag.AddDays(1));

        Assert.Equal(Gittstatus.GittIdag,
            (await tjeneste.Gitt(dyrId, idag, Idag.AddDays(2), default)).Status);
        Assert.Equal(1, await db.Behandling.CountAsync(b => b.DyrId == dyrId));

        // Ingen knapp noe sted: ikke i historikken, ikke pa dashbordet, og
        // dialogen forklarer i stedet for a tilby et skjema.
        Assert.False((await tjeneste.HentFor(dyrId, default)).Single().KanKrysseAv(Idag));
        Assert.False(Assert.Single((await Dashbord(db, h)).Forfaller,
            p => p.Kilde == Kilde.Behandling).KanFolgesOpp);
        Assert.False((await tjeneste.HentGittgrunnlag(dyrId, idag, default))!.KanGis);
    }

    [Fact]
    public async Task Gitt_pa_et_annet_dyr_finnes_ikke()
    {
        var h = await _fixture.OpprettHusstand("Feil dyr");
        await using var db = _fixture.LagContext(h);
        var luna = await NyttDyr(db, h);
        var milo = await NyttDyr(db, h, "Milo");

        var lunas = await Behandling(db, luna, BehandlingType.Ormekur, "Milbemax",
            Idag.AddMonths(-3), Idag);

        Assert.Equal(Gittstatus.FinnesIkke,
            (await new BehandlingService(db).Gitt(milo, lunas, null, default)).Status);
        Assert.Equal(1, await db.Behandling.CountAsync());
    }

    [Fact]
    public async Task Gitt_i_en_annen_husstand_finnes_ikke()
    {
        var a = await _fixture.OpprettHusstand("Gitt A");
        var b = await _fixture.OpprettHusstand("Gitt B");

        int dyrId, behandlingId;
        await using (var eier = _fixture.LagContext(a))
        {
            dyrId = await NyttDyr(eier, a);
            behandlingId = await Behandling(eier, dyrId, BehandlingType.Ormekur,
                "Milbemax", Idag.AddMonths(-3), Idag);
        }

        await using (var fremmed = _fixture.LagContext(b))
        {
            Assert.Equal(Gittstatus.FinnesIkke,
                (await new BehandlingService(fremmed).Gitt(dyrId, behandlingId, null, default))
                    .Status);
        }

        await using var igjen = _fixture.LagContext(a);
        Assert.Equal(1, await igjen.Behandling.CountAsync());
    }

    // --- Forslag ---------------------------------------------------------

    [Fact]
    public async Task Forslag_er_en_per_type_og_preparat_nyeste_forst_i_egen_husstand()
    {
        var a = await _fixture.OpprettHusstand("Forslag A");
        var b = await _fixture.OpprettHusstand("Forslag B");

        await using (var fremmed = _fixture.LagContext(b))
        {
            var fremmedDyr = await NyttDyr(fremmed, b);
            await Behandling(fremmed, fremmedDyr, BehandlingType.Vaksine,
                "Hemmelig", Idag, null);
        }

        await using var db = _fixture.LagContext(a);
        var luna = await NyttDyr(db, a);
        var milo = await NyttDyr(db, a, "Milo");

        await Behandling(db, luna, BehandlingType.Ormekur, "Milbemax",
            Idag.AddMonths(-6), Idag.AddMonths(-3));
        var nyesteOrmekur = await Behandling(db, luna, BehandlingType.Ormekur,
            "milbemax", Idag.AddMonths(-3), Idag);
        // Fra det andre dyret: forslagene gar pa tvers av dyrene.
        var vaksine = await Behandling(db, milo, BehandlingType.Vaksine,
            "Nobivac KC", Idag.AddDays(-1), Idag.AddYears(1));

        var forslag = await new BehandlingService(db).HentForslag(default);

        Assert.Equal([vaksine, nyesteOrmekur], forslag.Select(f => f.Id));
        Assert.DoesNotContain(forslag, f => f.Preparat == "Hemmelig");
    }

    // --- Medisin ---------------------------------------------------------

    [Fact]
    public async Task Medisin_som_forfaller_i_dag_star_pa_oversikten_til_dosen_er_gitt()
    {
        var h = await _fixture.OpprettHusstand("Dose oversikt");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var tjeneste = new MedisinService(db, NullLogger<MedisinService>.Instance);

        await tjeneste.Registrer(new NyMedisin(
            dyrId, "Apoquel", "1 tablett", 24, Idag.AddDays(-2), null), default);
        // Ved behov skal aldri varsles.
        await tjeneste.Registrer(new NyMedisin(
            dyrId, "Metacam", "1 ml", 0, Idag.AddDays(-2), null), default);

        var medisinId = await db.Medisin
            .Where(m => m.Navn == "Apoquel").Select(m => m.Id).SingleAsync();

        var dose = Assert.Single((await Dashbord(db, h)).Forfaller,
            p => p.Kilde == Kilde.Medisin);

        Assert.Equal(medisinId, dose.KildeId);
        Assert.Equal(dyrId, dose.DyrId);
        Assert.Contains("Apoquel", dose.Tekst);
        // Aldri gitt betyr at den forfaller na.
        Assert.True(dose.ErForfalt(Idag));

        Assert.True((await tjeneste.LoggDose(dyrId, medisinId, null, false, default)).Ok);

        // Neste dose er om 24 timer - i morgen.
        Assert.DoesNotContain((await Dashbord(db, h)).Forfaller,
            p => p.Kilde == Kilde.Medisin);
    }

    // --- Veterinaerkontroll ----------------------------------------------

    [Fact]
    public async Task Bestilt_kontroll_fjerner_paminnelsen_og_blir_en_time()
    {
        var h = await _fixture.OpprettHusstand("Kontroll");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var t = new VeterinarService(db, new Husstandskontekst { HusstandId = h });

        await t.OpprettBesok(new NyttVetbesok(
            dyrId, null, "Dyreklinikken", Idag.AddDays(-10), null, "Sårstell",
            null, 1200, false, null, Idag.AddDays(5), null), default);

        var fra = Assert.Single((await Dashbord(db, h)).Forfaller,
            p => p.Kilde == Kilde.Vetkontroll);

        Assert.NotNull(await t.BestillKontroll(fra.KildeId, new NyttVetbesok(
            dyrId, null, "Dyreklinikken", Idag.AddDays(5), new TimeOnly(10, 0),
            "Kontroll etter sårstell", null, null, false, null, null, null), default));

        var etter = (await Dashbord(db, h)).Forfaller;

        Assert.DoesNotContain(etter, p => p.Kilde == Kilde.Vetkontroll);
        var time = Assert.Single(etter, p => p.Kilde == Kilde.Vetbesok);
        Assert.Equal(Idag.AddDays(5), time.Dato);

        Assert.Null(await db.Vetbesok
            .Where(v => v.Id == fra.KildeId)
            .Select(v => v.NesteKontrollDato)
            .SingleAsync());
    }

    [Fact]
    public async Task Kontroll_for_et_annet_dyr_blir_ikke_rort()
    {
        var h = await _fixture.OpprettHusstand("Kontroll feil dyr");
        await using var db = _fixture.LagContext(h);
        var luna = await NyttDyr(db, h);
        var milo = await NyttDyr(db, h, "Milo");
        var t = new VeterinarService(db, new Husstandskontekst { HusstandId = h });

        await t.OpprettBesok(new NyttVetbesok(
            milo, null, null, Idag.AddDays(-10), null, "Halting",
            null, null, false, null, Idag.AddDays(5), null), default);

        var milosBesok = await db.Vetbesok.Where(v => v.DyrId == milo)
            .Select(v => v.Id).SingleAsync();

        // Timen for Luna lagres, men Milos kontroll star igjen.
        Assert.NotNull(await t.BestillKontroll(milosBesok, new NyttVetbesok(
            luna, null, null, Idag.AddDays(5), null, "Kontroll",
            null, null, false, null, null, null), default));

        Assert.Equal(Idag.AddDays(5), await db.Vetbesok
            .Where(v => v.Id == milosBesok)
            .Select(v => v.NesteKontrollDato)
            .SingleAsync());
    }

    // --- Over HTTP -------------------------------------------------------

    /// <summary>
    /// Knappen pa dashbordet, posten og veien tilbake. Tjenestetestene over
    /// beviser regelen, men ikke at knappen havner pa siden og peker riktig.
    /// </summary>
    [Fact]
    public async Task Gitt_fra_oversikten_apner_dialogen_og_sender_tilbake_til_oversikten()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var lagret = await klient.Post($"/dyr/{dyrId}/behandling", new Dictionary<string, string>
        {
            ["Type"] = BehandlingType.Ormekur.ToString(),
            ["Preparat"] = "Milbemax",
            ["Dato"] = Idag.AddMonths(-3).ToString("yyyy-MM-dd"),
            ["NesteDato"] = Idag.AddDays(-1).ToString("yyyy-MM-dd")
        });
        Assert.True(Skjemaklient.GikkGjennom(lagret));

        var oversikt = await (await klient.Hent("/")).Content.ReadAsStringAsync();
        var lenke = Gittmonster().Match(oversikt);

        Assert.True(lenke.Success, "Fant ikke Gitt-knappen pa oversikten.");
        var dialog = WebUtility.HtmlDecode(lenke.Groups[1].Value);
        Assert.StartsWith($"/dyr/{dyrId}/behandling/", dialog);

        // Uten htmx-hodet kommer dialogen som egen side - samme skjema.
        var side = await (await klient.Hent(dialog)).Content.ReadAsStringAsync();

        // Feltet star tomt. Appen gjetter ikke pa neste gang. Razor utelater
        // value-attributtet helt nar verdien er null.
        var felt = Regex.Match(side, """<input[^>]+name="NesteDato"[^>]*>""");
        Assert.True(felt.Success, "Fant ikke feltet for neste gang.");
        Assert.DoesNotContain("value=", felt.Value);

        // Brukeren fyller inn datoen selv.
        var valgt = Idag.AddDays(40);
        var svar = await klient.Post(
            dialog.Split('?')[0],
            new Dictionary<string, string>
            {
                ["NesteDato"] = valgt.ToString("yyyy-MM-dd"),
                ["FraOversikt"] = "true"
            },
            tokenFra: dialog);

        Assert.Equal(HttpStatusCode.Redirect, svar.StatusCode);
        Assert.Equal("/", svar.Headers.Location?.ToString());

        var etter = await (await klient.Hent("/")).Content.ReadAsStringAsync();
        Assert.DoesNotMatch(Gittmonster(), etter);

        var historikk = await (await klient.Hent($"/dyr/{dyrId}/behandling")).Content
            .ReadAsStringAsync();
        Assert.Contains($"neste {valgt:d. MMM yyyy}", WebUtility.HtmlDecode(historikk));
    }

    [Fact]
    public async Task Neste_gang_i_dag_avvises_og_ingenting_lagres()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        await klient.Post($"/dyr/{dyrId}/behandling", new Dictionary<string, string>
        {
            ["Type"] = BehandlingType.Ormekur.ToString(),
            ["Preparat"] = "Milbemax",
            ["Dato"] = Idag.AddMonths(-3).ToString("yyyy-MM-dd"),
            ["NesteDato"] = Idag.ToString("yyyy-MM-dd")
        });

        var side = await (await klient.Hent($"/dyr/{dyrId}/behandling")).Content
            .ReadAsStringAsync();
        var dialog = Regex.Match(side, $"""href="(/dyr/{dyrId}/behandling/\d+/gitt)" """.TrimEnd());
        Assert.True(dialog.Success, "Fant ikke Gitt i dag-knappen i historikken.");

        var svar = await klient.Post(
            dialog.Groups[1].Value,
            new Dictionary<string, string> { ["NesteDato"] = Idag.ToString("yyyy-MM-dd") });

        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        Assert.Contains("Neste gang må være etter i dag.",
            WebUtility.HtmlDecode(await svar.Content.ReadAsStringAsync()));

        var etter = await (await klient.Hent($"/dyr/{dyrId}/behandling")).Content
            .ReadAsStringAsync();
        // Ble noe lagret, ville den gamle raden vaert fulgt opp av en ny fra
        // i dag - og da ville knappen vaert borte.
        Assert.Contains(dialog.Groups[1].Value, etter);
    }

    [Fact]
    public async Task Gjenta_fyller_skjemaet_med_preparat_og_neste_gang()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        await klient.Post($"/dyr/{dyrId}/behandling", new Dictionary<string, string>
        {
            ["Type"] = BehandlingType.Vaksine.ToString(),
            ["Preparat"] = "Nobivac KC",
            ["Dato"] = Idag.AddYears(-1).ToString("yyyy-MM-dd"),
            ["NesteDato"] = Idag.ToString("yyyy-MM-dd")
        });

        var side = await (await klient.Hent($"/dyr/{dyrId}/behandling")).Content
            .ReadAsStringAsync();
        var lenke = Regex.Match(side, $"""href="(/dyr/{dyrId}/behandling\?gjenta=\d+)#skjema""");
        Assert.True(lenke.Success, "Fant ikke forslaget over skjemaet.");

        var skjema = await (await klient.Hent(lenke.Groups[1].Value)).Content
            .ReadAsStringAsync();

        Assert.Contains("""value="Nobivac KC" """.TrimEnd(), skjema);
        Assert.Contains(
            $"""value="{Idag.AddYears(1):yyyy-MM-dd}" """.TrimEnd(), skjema);
    }

    [GeneratedRegex("""<a[^>]+href="([^"]+/gitt\?fraOversikt=true)"[^>]*>""")]
    private static partial Regex Gittmonster();
}
