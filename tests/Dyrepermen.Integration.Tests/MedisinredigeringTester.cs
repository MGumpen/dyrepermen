using System.Net;
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
/// Avslutt og rediger pa medisiner. "Avslutt" skal virke med en gang, ikke
/// ved midnatt - se ADR 0017. Rediger retter medisinen uten a rore
/// doseloggen, slik at en dose kan trappes ned.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class MedisinredigeringTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public MedisinredigeringTester(DatabaseFixture fixture) => _fixture = fixture;

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

    private static MedisinService Tjeneste(DyrepermenDbContext db)
        => new(db, NullLogger<MedisinService>.Instance);

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

    private static async Task<int> NyMedisin(
        DyrepermenDbContext db, int dyrId, string navn,
        DateOnly start, DateOnly? slutt, int intervall = 24)
    {
        await Tjeneste(db).Registrer(
            new NyMedisin(dyrId, navn, "1 tablett", intervall, start, slutt), default);
        return await db.Medisin.Where(m => m.Navn == navn && m.DyrId == dyrId)
            .Select(m => m.Id).SingleAsync();
    }

    private static DyrService DyrTjeneste(DyrepermenDbContext db, int husstand)
        => new(db, new Husstandskontekst { HusstandId = husstand },
            NullLogger<DyrService>.Instance);

    private static Task<Dashbord> Dashbord(DyrepermenDbContext db, int husstand)
        => new DashbordService(
            db, new HandlelisteService(db,
                new Husstandskontekst { HusstandId = husstand })).Hent(default);

    // --- Avslutt ---------------------------------------------------------

    /// <summary>
    /// Feilen som ble meldt: "Avslutt" satte sluttdatoen til i dag, og siden
    /// sluttdatoen er til og med, var medisinen aktiv resten av dagen - med
    /// "Gi dose" og neste dose pa siden og pa dashbordet.
    /// </summary>
    [Fact]
    public async Task Avslutt_virker_med_en_gang_overalt()
    {
        var h = await _fixture.OpprettHusstand("Avslutt na");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var tjeneste = Tjeneste(db);

        var id = await NyMedisin(db, dyrId, "Apoquel", Idag.AddDays(-5), null);

        Assert.Single((await Dashbord(db, h)).Forfaller, p => p.Kilde == Kilde.Medisin);

        Assert.True(await tjeneste.Avslutt(dyrId, id, default));

        var rad = Assert.Single(await tjeneste.HentFor(dyrId, default));
        Assert.True(rad.ErAvsluttet(Idag));
        Assert.NotNull(rad.AvsluttetTid);
        // Perioden viser at kuren sluttet i dag.
        Assert.Equal(Idag, rad.SluttDato);

        Assert.DoesNotContain((await Dashbord(db, h)).Forfaller, p => p.Kilde == Kilde.Medisin);
        Assert.Empty((await DyrTjeneste(db, h).HentSammendrag(dyrId, default))!.AktiveMedisiner);

        // Serveren avviser en dose, selv om knappen skulle sta i en gammel fane.
        Assert.False((await tjeneste.LoggDose(dyrId, id, null, true, default)).Ok);
        Assert.Equal(0, await db.Dose.CountAsync(d => d.MedisinId == id));
    }

    [Fact]
    public async Task Avslutt_beholder_en_sluttdato_som_allerede_har_passert()
    {
        var h = await _fixture.OpprettHusstand("Avslutt gammel");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var id = await NyMedisin(db, dyrId, "Metacam", Idag.AddDays(-20), Idag.AddDays(-10));

        await Tjeneste(db).Avslutt(dyrId, id, default);

        Assert.Equal(Idag.AddDays(-10),
            (await Tjeneste(db).HentFor(dyrId, default)).Single().SluttDato);
    }

    /// <summary>
    /// Regelen for "aktiv" star to steder: Medisinfilter i sporringene og
    /// MedisinRad.ErAvsluttet i minnet. Testen feiler nar de spriker.
    /// </summary>
    [Fact]
    public async Task Regelen_i_sporringen_og_i_minnet_er_den_samme()
    {
        var h = await _fixture.OpprettHusstand("Aktiv regel");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var tjeneste = Tjeneste(db);

        await NyMedisin(db, dyrId, "Pagaende", Idag.AddDays(-5), null);
        await NyMedisin(db, dyrId, "Slutter i dag", Idag.AddDays(-5), Idag);
        await NyMedisin(db, dyrId, "Sluttet i gar", Idag.AddDays(-5), Idag.AddDays(-1));
        await NyMedisin(db, dyrId, "Slutter senere", Idag.AddDays(-5), Idag.AddDays(5));
        var avsluttet = await NyMedisin(db, dyrId, "Avsluttet", Idag.AddDays(-5), Idag.AddDays(5));
        await tjeneste.Avslutt(dyrId, avsluttet, default);

        var iMinnet = (await tjeneste.HentFor(dyrId, default))
            .Where(m => !m.ErAvsluttet(Idag))
            .Select(m => m.Navn)
            .Order()
            .ToList();

        var iSporringen = (await DyrTjeneste(db, h).HentSammendrag(dyrId, default))!
            .AktiveMedisiner
            .Select(m => m.Split(" – ")[0])
            .Order()
            .ToList();

        Assert.Equal(["Pagaende", "Slutter i dag", "Slutter senere"], iMinnet);
        Assert.Equal(iMinnet, iSporringen);
    }

    // --- Rediger ---------------------------------------------------------

    [Fact]
    public async Task Rediger_endrer_dosen_og_lar_doseloggen_sta()
    {
        var h = await _fixture.OpprettHusstand("Nedtrapping");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var tjeneste = Tjeneste(db);

        var id = await NyMedisin(db, dyrId, "Prednisolon", Idag.AddDays(-10), null, intervall: 12);
        Assert.True((await tjeneste.LoggDose(dyrId, id, null, false, default)).Ok);

        Assert.True(await tjeneste.Oppdater(dyrId, id, new NyMedisin(
            dyrId, "Prednisolon", "1/2 tablett", 24, Idag.AddDays(-10), Idag.AddDays(14)),
            default));

        var rad = Assert.Single(await tjeneste.HentFor(dyrId, default));
        Assert.Equal("1/2 tablett", rad.Dose);
        Assert.Equal(24, rad.IntervallTimer);
        Assert.Equal(Idag.AddDays(14), rad.SluttDato);
        Assert.Equal(1, await db.Dose.CountAsync(d => d.MedisinId == id));
    }

    [Fact]
    public async Task Medisin_pa_et_annet_dyr_lar_seg_ikke_redigere()
    {
        var h = await _fixture.OpprettHusstand("Rediger feil dyr");
        await using var db = _fixture.LagContext(h);
        var luna = await NyttDyr(db, h);
        var milo = await NyttDyr(db, h, "Milo");

        var id = await NyMedisin(db, luna, "Apoquel", Idag, null);

        Assert.False(await Tjeneste(db).Oppdater(milo, id, new NyMedisin(
            milo, "Endret", "2 tabletter", 12, Idag, null), default));
        Assert.Equal("Apoquel", await db.Medisin.Where(m => m.Id == id)
            .Select(m => m.Navn).SingleAsync());
    }

    [Fact]
    public async Task Medisin_i_en_annen_husstand_lar_seg_ikke_redigere()
    {
        var a = await _fixture.OpprettHusstand("Rediger A");
        var b = await _fixture.OpprettHusstand("Rediger B");

        int dyrId, id;
        await using (var eier = _fixture.LagContext(a))
        {
            dyrId = await NyttDyr(eier, a);
            id = await NyMedisin(eier, dyrId, "Apoquel", Idag, null);
        }

        await using (var fremmed = _fixture.LagContext(b))
        {
            Assert.False(await Tjeneste(fremmed).Oppdater(dyrId, id, new NyMedisin(
                dyrId, "Endret", "2 tabletter", 12, Idag, null), default));
        }

        await using var igjen = _fixture.LagContext(a);
        Assert.Equal("Apoquel", await igjen.Medisin.Select(m => m.Navn).SingleAsync());
    }

    // --- Over HTTP -------------------------------------------------------

    [Fact]
    public async Task Rediger_fyller_skjemaet_og_lagrer_endringen()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);
        var side = $"/dyr/{dyrId}/medisin";

        await klient.Post(side, new Dictionary<string, string>
        {
            ["Navn"] = "Prednisolon",
            ["Dose"] = "1 tablett",
            ["IntervallTimer"] = "12",
            ["StartDato"] = Idag.ToString("yyyy-MM-dd")
        });

        var liste = await (await klient.Hent(side)).Content.ReadAsStringAsync();
        var lenke = System.Text.RegularExpressions.Regex.Match(
            liste, $"""href="({side}\?rediger=\d+)#skjema""");
        Assert.True(lenke.Success, "Fant ikke Rediger-lenken.");

        var skjema = WebUtility.HtmlDecode(
            await (await klient.Hent(lenke.Groups[1].Value)).Content.ReadAsStringAsync());
        Assert.Contains("Endre medisin", skjema);
        Assert.Contains("value=\"1 tablett\"", skjema);

        var medisinId = lenke.Groups[1].Value.Split('=').Last();
        var svar = await klient.Post($"{side}/{medisinId}/rediger", new Dictionary<string, string>
        {
            ["Navn"] = "Prednisolon",
            ["Dose"] = "1/2 tablett",
            ["IntervallTimer"] = "24",
            ["StartDato"] = Idag.ToString("yyyy-MM-dd")
        }, tokenFra: lenke.Groups[1].Value);

        Assert.True(Skjemaklient.GikkGjennom(svar),
            $"Endringen ble ikke lagret: {await Skjemaklient.Feilmeldinger(svar)}");

        var etter = WebUtility.HtmlDecode(
            await (await klient.Hent(side)).Content.ReadAsStringAsync());
        Assert.Contains("1/2 tablett", etter);
        Assert.Contains("Hver 24. time", etter);
    }

    [Fact]
    public async Task Til_for_fra_avvises()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var svar = await klient.Post($"/dyr/{dyrId}/medisin", new Dictionary<string, string>
        {
            ["Navn"] = "Apoquel",
            ["Dose"] = "1 tablett",
            ["StartDato"] = Idag.ToString("yyyy-MM-dd"),
            ["SluttDato"] = Idag.AddDays(-1).ToString("yyyy-MM-dd")
        });

        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        Assert.Contains("Til-datoen kan ikke være før fra-datoen.",
            await Skjemaklient.Feilmeldinger(svar));
    }

    [Fact]
    public async Task Avsluttet_medisin_har_ingen_knapp_for_dose_eller_neste_dose()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);
        var side = $"/dyr/{dyrId}/medisin";

        await klient.Post(side, new Dictionary<string, string>
        {
            ["Navn"] = "Apoquel",
            ["Dose"] = "1 tablett",
            ["IntervallTimer"] = "12",
            ["StartDato"] = Idag.ToString("yyyy-MM-dd")
        });

        var liste = await (await klient.Hent(side)).Content.ReadAsStringAsync();
        var dose = System.Text.RegularExpressions.Regex.Match(
            liste, $"""action="({side}/\d+)/dose""");
        Assert.True(dose.Success);

        await klient.Post($"{dose.Groups[1].Value}/dose", [], tokenFra: side);
        await klient.Post($"{dose.Groups[1].Value}/avslutt", [], tokenFra: side);

        var etter = WebUtility.HtmlDecode(
            await (await klient.Hent(side)).Content.ReadAsStringAsync());

        Assert.Contains("avsluttet", etter);
        Assert.DoesNotContain("/dose\"", etter);
        Assert.DoesNotContain("Neste tidligst", etter);
        Assert.DoesNotContain("/avslutt\"", etter);
    }
}
