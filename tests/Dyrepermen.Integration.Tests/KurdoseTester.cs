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

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Behandlinger over flere dager eller flere ganger per dag: doser som gis
/// fra oversikten, og reglene tjenesten handhever - aktiv i dag, og hoyst
/// sa mange doser per dag som kuren sier, ogsa nar to trykker samtidig.
/// Se ADR 0019.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed partial class KurdoseTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public KurdoseTester(DatabaseFixture fixture) => _fixture = fixture;

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

    [GeneratedRegex("""id="kur-(\d+)""")]
    private static partial Regex Kurmonster();

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

    private static async Task<int> Kur(
        DyrepermenDbContext db, int dyrId, DateOnly forsteDag,
        int antallDager, int gangerPerDag, DateOnly? neste = null)
    {
        var rad = new Behandling
        {
            DyrId = dyrId,
            Type = BehandlingType.Ormekur,
            Preparat = "Milbemax",
            Dato = forsteDag,
            NesteDato = neste,
            AntallDager = antallDager,
            GangerPerDag = gangerPerDag
        };
        db.Behandling.Add(rad);
        await db.SaveChangesAsync();
        return rad.Id;
    }

    private static Task<Dashbord> Dashbord(DyrepermenDbContext db, int husstand)
        => new DashbordService(
            db, new HandlelisteService(db,
                new Husstandskontekst { HusstandId = husstand })).Hent(default);

    /// <summary>
    /// Selve lasen. Hver tjeneste har sin egen tilkobling, apnet for
    /// startskuddet, slik to telefoner i samme husstand ville hatt. Uten
    /// FOR UPDATE teller alle 0 og legger inn hver sin dose.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Samtidige_trykk_gir_aldri_flere_doser_enn_ganger_per_dag(
        int gangerPerDag)
    {
        const int Samtidige = 8;

        var h = await _fixture.OpprettHusstand("Samtidig");
        int dyrId;
        int kurId;

        await using (var db = _fixture.LagContext(h))
        {
            dyrId = await NyttDyr(db, h);
            kurId = await Kur(db, dyrId, Idag, 3, gangerPerDag);
        }

        var kontekster = Enumerable.Range(0, Samtidige)
            .Select(_ => _fixture.LagContext(h))
            .ToList();

        try
        {
            foreach (var db in kontekster)
            {
                await db.Database.OpenConnectionAsync();
            }

            var start = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);

            var trykk = kontekster
                .Select(async db =>
                {
                    await start.Task;
                    return await new BehandlingService(db)
                        .LoggDose(dyrId, kurId, null, default);
                })
                .ToList();

            start.SetResult();
            var svar = await Task.WhenAll(trykk);

            Assert.Equal(gangerPerDag, svar.Count(s => s == Kurdosestatus.Lagret));
            Assert.Equal(
                Samtidige - gangerPerDag,
                svar.Count(s => s == Kurdosestatus.FullfortIdag));
        }
        finally
        {
            foreach (var db in kontekster)
            {
                await db.DisposeAsync();
            }
        }

        await using var etter = _fixture.LagContext(h);
        Assert.Equal(gangerPerDag,
            await etter.BehandlingDose.CountAsync(d => d.BehandlingId == kurId));
    }

    [Fact]
    public async Task Dose_avvises_utenfor_perioden()
    {
        var h = await _fixture.OpprettHusstand("Utenfor perioden");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var tjeneste = new BehandlingService(db);

        // Starter i morgen, og en som var ferdig i gar.
        var framtidig = await Kur(db, dyrId, Idag.AddDays(1), 3, 1);
        var ferdig = await Kur(db, dyrId, Idag.AddDays(-3), 3, 1);

        Assert.Equal(Kurdosestatus.IkkeAktiv,
            await tjeneste.LoggDose(dyrId, framtidig, null, default));
        Assert.Equal(Kurdosestatus.IkkeAktiv,
            await tjeneste.LoggDose(dyrId, ferdig, null, default));

        Assert.Equal(0, await db.BehandlingDose.CountAsync());
    }

    [Fact]
    public async Task Siste_dag_tar_fortsatt_imot_doser()
    {
        var h = await _fixture.OpprettHusstand("Siste dag");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var kur = await Kur(db, dyrId, Idag.AddDays(-2), 3, 1);

        Assert.Equal(Kurdosestatus.Lagret,
            await new BehandlingService(db).LoggDose(dyrId, kur, null, default));
    }

    [Fact]
    public async Task Vanlig_behandling_tar_ikke_imot_doser()
    {
        // En dag og en gang regnes som gitt nar den registreres.
        var h = await _fixture.OpprettHusstand("Vanlig");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var vanlig = await Kur(db, dyrId, Idag, 1, 1);

        Assert.Equal(Kurdosestatus.IkkeAktiv,
            await new BehandlingService(db).LoggDose(dyrId, vanlig, null, default));
        Assert.Equal(0, await db.BehandlingDose.CountAsync());
    }

    /// <summary>
    /// Query-filteret beskytter lesing, ikke skriving. Tjenesten ma sla opp
    /// kuren gjennom filteret for den lases og en dose legges inn.
    /// </summary>
    [Fact]
    public async Task Fremmed_husstand_finner_ikke_kuren()
    {
        var a = await _fixture.OpprettHusstand("Eier");
        var b = await _fixture.OpprettHusstand("Fremmed");
        int dyrId;
        int kur;

        await using (var db = _fixture.LagContext(a))
        {
            dyrId = await NyttDyr(db, a);
            kur = await Kur(db, dyrId, Idag, 3, 1);
        }

        await using (var fremmed = _fixture.LagContext(b))
        {
            Assert.Equal(Kurdosestatus.FinnesIkke,
                await new BehandlingService(fremmed).LoggDose(dyrId, kur, null, default));
            Assert.Null(await new BehandlingService(fremmed).HentKurrad(dyrId, kur, default));
        }

        await using var eier = _fixture.LagContext(a);
        Assert.Equal(0, await eier.BehandlingDose.CountAsync());
    }

    [Fact]
    public async Task Kur_pa_et_annet_dyr_i_egen_husstand_treffer_ikke()
    {
        var h = await _fixture.OpprettHusstand("To dyr");
        await using var db = _fixture.LagContext(h);
        var luna = await NyttDyr(db, h, "Luna");
        var milo = await NyttDyr(db, h, "Milo");

        var lunas = await Kur(db, luna, Idag, 3, 1);

        Assert.Equal(Kurdosestatus.FinnesIkke,
            await new BehandlingService(db).LoggDose(milo, lunas, null, default));
    }

    [Fact]
    public async Task Oversikten_viser_kuren_med_fremdrift()
    {
        var h = await _fixture.OpprettHusstand("Fremdrift");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var kur = await Kur(db, dyrId, Idag.AddDays(-1), 3, 2);

        // En dose i gar kveld, rett for norsk midnatt, og en i dag. Bare den
        // siste skal telle.
        var dagStart = Tidssone.DagStart(DateTimeOffset.UtcNow);
        db.BehandlingDose.AddRange(
            new BehandlingDose { BehandlingId = kur, GittTid = dagStart.AddMinutes(-30) },
            new BehandlingDose { BehandlingId = kur, GittTid = dagStart.AddMinutes(1) });
        await db.SaveChangesAsync();

        // Ikke pa oversikten: en vanlig behandling i dag, en kur som var
        // ferdig i gar, og en som starter i morgen.
        await Kur(db, dyrId, Idag, 1, 1);
        await Kur(db, dyrId, Idag.AddDays(-3), 3, 1);
        await Kur(db, dyrId, Idag.AddDays(1), 3, 1);

        var rad = Assert.Single(
            (await Dashbord(db, h)).Forfaller, p => p.Kilde == Kilde.Kur);

        Assert.Equal(kur, rad.KildeId);
        Assert.Equal("Ormekur – Milbemax – dag 2 av 3 · 1/2 i dag", rad.Tekst);
        Assert.True(rad.KanFolgesOpp);
        Assert.False(rad.ErForfalt(Idag));
        Assert.Equal(1, rad.Kur!.DoserIdag);
    }

    /// <summary>
    /// Regelen for "aktiv kur" star to steder: Behandlingsfilter.AktivKur i
    /// dashbordets sporring og Behandlingskur i minnet, som LoggDose bruker.
    /// Spriker de, viser oversikten en knapp tjenesten avviser - eller
    /// skjuler en tjenesten ville godtatt. Testen feiler nar de spriker.
    /// </summary>
    [Fact]
    public async Task Regelen_i_sporringen_og_i_minnet_er_den_samme()
    {
        var h = await _fixture.OpprettHusstand("Aktiv kur");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        (DateOnly ForsteDag, int Dager, int Ganger)[] tilfeller =
        [
            (Idag, 1, 1),
            (Idag, 1, 2),
            (Idag, 3, 1),
            (Idag.AddDays(-2), 3, 1),
            (Idag.AddDays(-3), 3, 1),
            (Idag.AddDays(1), 3, 1),
            (Idag.AddDays(-1), 1, 3)
        ];

        var iMinnet = new List<int>();

        foreach (var (forsteDag, dager, ganger) in tilfeller)
        {
            var id = await Kur(db, dyrId, forsteDag, dager, ganger);

            if (Behandlingskur.ErKur(dager, ganger)
                && Behandlingskur.ErAktiv(forsteDag, dager, Idag))
            {
                iMinnet.Add(id);
            }
        }

        var iSporringen = (await Dashbord(db, h)).Forfaller
            .Where(p => p.Kilde == Kilde.Kur)
            .Select(p => p.KildeId)
            .Order()
            .ToList();

        Assert.Equal(3, iMinnet.Count);
        Assert.Equal(iMinnet, iSporringen);
    }

    [Fact]
    public async Task Fullfort_dag_star_igjen_uten_knapp()
    {
        var h = await _fixture.OpprettHusstand("Fullfort");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var kur = await Kur(db, dyrId, Idag, 3, 1);
        Assert.Equal(Kurdosestatus.Lagret,
            await new BehandlingService(db).LoggDose(dyrId, kur, null, default));

        var rad = Assert.Single(
            (await Dashbord(db, h)).Forfaller, p => p.Kilde == Kilde.Kur);

        Assert.True(rad.Kur!.FullfortIdag);
        Assert.False(rad.KanFolgesOpp);
    }

    /// <summary>
    /// "Gitt i dag" pa paminnelsen om neste kur: den nye er en kur den ogsa,
    /// og dagens forste dose er gitt - ellers ville oversikten sagt "0/1 i
    /// dag" rett etter at brukeren sa at den var gitt.
    /// </summary>
    [Fact]
    public async Task Gitt_pa_en_kur_gir_ny_kur_med_forste_dose()
    {
        var h = await _fixture.OpprettHusstand("Neste kur");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var forrige = await Kur(db, dyrId, Idag.AddMonths(-3), 3, 1, neste: Idag);

        Assert.Equal(Gittstatus.Lagret,
            (await new BehandlingService(db).Gitt(dyrId, forrige, null, null, default)).Status);

        var ny = await db.Behandling.AsNoTracking()
            .Include(b => b.Doser)
            .SingleAsync(b => b.DyrId == dyrId && b.Id != forrige);

        Assert.Equal(3, ny.AntallDager);
        Assert.Equal(1, ny.GangerPerDag);
        Assert.Single(ny.Doser);

        var rad = Assert.Single(
            (await Dashbord(db, h)).Forfaller, p => p.Kilde == Kilde.Kur);
        Assert.Equal("Ormekur – Milbemax – dag 1 av 3 · 1/1 i dag", rad.Tekst);
    }

    /// <summary>
    /// Hele veien over HTTP: skjemaet lagrer kuren, oversikten tegner knappen,
    /// og knappen med htmx far tilbake bare raden - ikke hele siden.
    /// </summary>
    [Fact]
    public async Task Kur_registreres_i_skjemaet_og_gis_fra_oversikten()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var lagret = await klient.Post(
            $"/dyr/{dyrId}/behandling",
            new Dictionary<string, string>
            {
                // Navnet, ikke tallet. Nedtrekkslisten skriver ut enumnavnet.
                ["Type"] = BehandlingType.Ormekur.ToString(),
                ["Preparat"] = "Milbemax",
                ["Dato"] = Idag.ToString("yyyy-MM-dd"),
                ["AntallDager"] = "3",
                ["GangerPerDag"] = "1"
            });

        Assert.True(
            Skjemaklient.GikkGjennom(lagret),
            $"Kuren ble ikke lagret: {await Skjemaklient.Feilmeldinger(lagret)}");

        var historikk = await Side(klient, $"/dyr/{dyrId}/behandling");
        Assert.Contains("1 gang daglig i 3 dager", historikk);

        var oversikt = await Side(klient, "/");
        Assert.Contains("dag 1 av 3 · 0/1 i dag", oversikt);
        var kurId = int.Parse(Kurmonster().Match(oversikt).Groups[1].Value);

        var svar = await klient.PostHtmx($"/dashbord/kur/{dyrId}/{kurId}", tokenFra: "/");
        var rad = WebUtility.HtmlDecode(await svar.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        Assert.Contains("1/1 i dag", rad);
        Assert.Contains("fullført i dag", rad);
        Assert.DoesNotContain("<html", rad);

        // Et trykk til fra en fane som har statt apen: raden tegnes slik den
        // er, og ingen dose til legges inn.
        var igjen = await klient.PostHtmx($"/dashbord/kur/{dyrId}/{kurId}", tokenFra: "/");
        Assert.Equal(HttpStatusCode.OK, igjen.StatusCode);
        Assert.Contains("1/1 i dag",
            WebUtility.HtmlDecode(await igjen.Content.ReadAsStringAsync()));
    }

    [Fact]
    public async Task Uten_htmx_sendes_brukeren_tilbake_til_oversikten()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);
        var kurId = await KurOverHttp(klient, dyrId);

        var svar = await klient.Post(
            $"/dashbord/kur/{dyrId}/{kurId}", [], tokenFra: "/");

        Assert.Equal(HttpStatusCode.Redirect, svar.StatusCode);
        Assert.Equal("/", svar.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Fremmed_husstand_far_404_pa_knappen()
    {
        var eier = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(eier);
        var kurId = await KurOverHttp(eier, dyrId);

        var fremmed = await Testoppsett.InnloggetKlient(_app);
        var svar = await fremmed.Post(
            $"/dashbord/kur/{dyrId}/{kurId}", [], tokenFra: "/");

        Assert.Equal(HttpStatusCode.NotFound, svar.StatusCode);
    }

    private static async Task<int> KurOverHttp(Skjemaklient klient, int dyrId)
    {
        var lagret = await klient.Post(
            $"/dyr/{dyrId}/behandling",
            new Dictionary<string, string>
            {
                ["Type"] = BehandlingType.Ormekur.ToString(),
                ["Dato"] = Idag.ToString("yyyy-MM-dd"),
                ["AntallDager"] = "3"
            });

        Assert.True(
            Skjemaklient.GikkGjennom(lagret),
            $"Kuren ble ikke lagret: {await Skjemaklient.Feilmeldinger(lagret)}");

        return int.Parse(Kurmonster().Match(await Side(klient, "/")).Groups[1].Value);
    }

    /// <summary>
    /// Dekoder HTML-entitetene. Standardencoderen koder alt utenfor ASCII,
    /// sa "fullført" star i kilden som &#xF8; midt i ordet.
    /// </summary>
    private static async Task<string> Side(Skjemaklient klient, string sti)
        => WebUtility.HtmlDecode(
            await (await klient.Hent(sti)).Content.ReadAsStringAsync());
}
