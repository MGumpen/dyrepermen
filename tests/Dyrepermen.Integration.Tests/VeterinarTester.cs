using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Services;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Dyrepermen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Integration.Tests;

[Collection(Databasesamling.Navn)]
public sealed class VeterinarTester
{
    private readonly DatabaseFixture _fixture;

    public VeterinarTester(DatabaseFixture fixture) => _fixture = fixture;

    private static VeterinarService Tjeneste(DyrepermenDbContext db, int husstand)
        => new(db, new Husstandskontekst { HusstandId = husstand });

    private static async Task<int> NyttDyr(DyrepermenDbContext db, int husstand)
    {
        var dyr = new Dyr
        {
            HusstandId = husstand,
            Navn = "Luna",
            Art = Art.Hund,
            Kjonn = Kjonn.Tispe
        };
        db.Dyr.Add(dyr);
        await db.SaveChangesAsync();
        return dyr.Id;
    }

    private static NyVeterinar Sted(string navn, Veterinartype type)
        => new(navn, type, "55 12 34 56", null, null, null,
               Apningstider.Tom, null);

    [Fact]
    public async Task Husstand_ser_ikke_annen_husstands_veterinaerer()
    {
        // Veterinar filtreres pa EGEN husstand_id-kolonne, ikke via dyret.
        // Den varianten ma testes for seg. Jf. plan kapittel 17.3.
        var a = await _fixture.OpprettHusstand("Hjemme vet");
        var b = await _fixture.OpprettHusstand("Naboen vet");

        await using (var eier = _fixture.LagContext(a))
        {
            await Tjeneste(eier, a).Opprett(
                Sted("Bergen Dyreklinikk", Veterinartype.Fast), default);
        }

        await using (var fremmed = _fixture.LagContext(b))
        {
            Assert.Empty(await Tjeneste(fremmed, b).Hent(default));
        }

        // Uten denne halvdelen ville et filter som gir tomt for alle bestatt.
        await using var igjen = _fixture.LagContext(a);
        Assert.Equal(
            "Bergen Dyreklinikk",
            (await Tjeneste(igjen, a).Hent(default)).Single().Navn);
    }

    [Fact]
    public async Task Husstandsiden_settes_fra_konteksten_ikke_fra_skjemaet()
    {
        // NyVeterinar har ingen HusstandId i det hele tatt - det er selve
        // garantien. Kom den fra klienten, kunne noen lagt en veterinaer inn
        // i en fremmed husstand.
        var h = await _fixture.OpprettHusstand("Kontekst");
        await using var db = _fixture.LagContext(h);

        await Tjeneste(db, h).Opprett(Sted("Vakta", Veterinartype.Vakt), default);

        Assert.Equal(h, (await db.Veterinar.SingleAsync()).HusstandId);
    }

    [Fact]
    public async Task Typen_sorterer_listen_ikke_navnet()
    {
        // Fast forst, sa vakt, sa sykehus. Alfabetisk ville lagt
        // "Akuttvakta" over fastveterinaeren.
        var h = await _fixture.OpprettHusstand("Sortering");
        await using var db = _fixture.LagContext(h);
        var t = Tjeneste(db, h);

        await t.Opprett(Sted("Osloveien Dyresykehus", Veterinartype.Sykehus), default);
        await t.Opprett(Sted("Akuttvakta", Veterinartype.Vakt), default);
        await t.Opprett(Sted("Vestkanten Dyreklinikk", Veterinartype.Fast), default);

        Assert.Equal(
            ["Vestkanten Dyreklinikk", "Akuttvakta", "Osloveien Dyresykehus"],
            (await t.Hent(default)).Select(v => v.Navn));
    }

    [Fact]
    public async Task Sletting_av_sted_beholder_besokene()
    {
        // Historikken skal ikke forsvinne fordi klinikken byttet navn eller
        // ble fjernet fra lista. Fremmednokkelen er SetNull.
        var h = await _fixture.OpprettHusstand("Slett sted");
        await using var db = _fixture.LagContext(h);
        var t = Tjeneste(db, h);
        var dyrId = await NyttDyr(db, h);

        await t.Opprett(Sted("Gamle klinikken", Veterinartype.Fast), default);
        var stedId = (await t.Hent(default)).Single().Id;

        Assert.True(await t.OpprettBesok(new NyttVetbesok(
            dyrId, stedId, null, new DateOnly(2026, 3, 4), null,
            "Vaksine", null, 1450, false, null, null, null), default));

        Assert.True(await t.Slett(stedId, default));

        db.ChangeTracker.Clear();
        var besok = (await t.HentBesok(default)).Single();

        Assert.Null(besok.VeterinarId);
        Assert.Equal(1450, besok.KostnadKr);
        Assert.Equal("Vaksine", besok.Arsak);
    }

    [Fact]
    public async Task Besok_kan_ikke_peke_pa_annen_husstands_sted()
    {
        // Uten sjekken kunne navnet pa en fremmed klinikk lekket ut gjennom
        // besokslisten.
        var a = await _fixture.OpprettHusstand("Eier sted");
        var b = await _fixture.OpprettHusstand("Fremmed sted");

        int stedId;
        await using (var eier = _fixture.LagContext(a))
        {
            await Tjeneste(eier, a).Opprett(Sted("Privat", Veterinartype.Fast), default);
            stedId = (await Tjeneste(eier, a).Hent(default)).Single().Id;
        }

        await using var fremmed = _fixture.LagContext(b);
        var dyrId = await NyttDyr(fremmed, b);

        Assert.False(await Tjeneste(fremmed, b).OpprettBesok(new NyttVetbesok(
            dyrId, stedId, null, new DateOnly(2026, 5, 1), null,
            "Forsøk", null, null, false, null, null, null), default));
    }

    [Fact]
    public async Task Besok_pa_annen_husstands_dyr_avvises()
    {
        var a = await _fixture.OpprettHusstand("Eier dyr vet");
        var b = await _fixture.OpprettHusstand("Fremmed dyr vet");

        int dyrId;
        await using (var eier = _fixture.LagContext(a))
        {
            dyrId = await NyttDyr(eier, a);
        }

        await using var fremmed = _fixture.LagContext(b);

        Assert.False(await Tjeneste(fremmed, b).OpprettBesok(new NyttVetbesok(
            dyrId, null, null, new DateOnly(2026, 5, 1), null,
            "Forsøk", null, null, false, null, null, null), default));
    }

    [Fact]
    public async Task Refusjon_uten_krav_nulles_framfor_a_kaste()
    {
        // Databasen har et CHECK-vilkar som avviser refusjon uten krav.
        // Tjenesten skal ikke la en skjemafeil bli til en 500-side.
        var h = await _fixture.OpprettHusstand("Refusjon");
        await using var db = _fixture.LagContext(h);
        var t = Tjeneste(db, h);
        var dyrId = await NyttDyr(db, h);

        Assert.True(await t.OpprettBesok(new NyttVetbesok(
            dyrId, null, "Et sted", new DateOnly(2026, 2, 2), null,
            "Kontroll", null, 900,
            ForsikringKrevd: false, RefundertKr: 700,
            null, null), default));

        Assert.Null((await t.HentBesok(default)).Single().RefundertKr);
    }

    [Fact]
    public async Task Kommende_og_tidligere_skilles_pa_dato_alene()
    {
        // Ingen statuskolonne. En status matte hukes av manuelt, og den som
        // glemte det ville hatt en "kommende" time fra i fjor staende.
        var h = await _fixture.OpprettHusstand("Kommende");
        await using var db = _fixture.LagContext(h);
        var t = Tjeneste(db, h);
        var dyrId = await NyttDyr(db, h);

        var idag = DateOnly.FromDateTime(DateTime.Now);

        await t.OpprettBesok(new NyttVetbesok(
            dyrId, null, null, idag.AddDays(7), new TimeOnly(8, 15),
            "Årskontroll", null, null, false, null, null, null), default);

        await t.OpprettBesok(new NyttVetbesok(
            dyrId, null, null, idag.AddDays(-30), null,
            "Halting", "Forstuing", 2300, true, 1800, null, null), default);

        var besok = await t.HentBesok(default);

        var kommende = besok.Single(b => b.ErKommende(idag));
        var tidligere = besok.Single(b => !b.ErKommende(idag));

        Assert.Equal("Årskontroll", kommende.Arsak);
        Assert.Null(kommende.KostnadKr);
        Assert.Equal(new TimeOnly(8, 15), kommende.Klokkeslett);

        // Netto er det husstanden faktisk satt igjen med.
        Assert.Equal(500, tidligere.NettoKr);
    }

    [Fact]
    public async Task Stedets_navn_vinner_over_friteksten()
    {
        // Star begge, skal visningen ha ett sted a sporre - ikke velge.
        var h = await _fixture.OpprettHusstand("Sted vinner");
        await using var db = _fixture.LagContext(h);
        var t = Tjeneste(db, h);
        var dyrId = await NyttDyr(db, h);

        await t.Opprett(Sted("Registrert klinikk", Veterinartype.Fast), default);
        var stedId = (await t.Hent(default)).Single().Id;

        await t.OpprettBesok(new NyttVetbesok(
            dyrId, stedId, "Skrevet inn for hand", new DateOnly(2026, 1, 5),
            null, "Kontroll", null, 500, false, null, null, null), default);

        Assert.Equal("Registrert klinikk", (await t.HentBesok(default)).Single().Sted);
    }

    [Fact]
    public async Task Tomt_navn_og_tom_arsak_avvises()
    {
        var h = await _fixture.OpprettHusstand("Tomt vet");
        await using var db = _fixture.LagContext(h);
        var t = Tjeneste(db, h);
        var dyrId = await NyttDyr(db, h);

        Assert.False(await t.Opprett(
            new NyVeterinar("   ", Veterinartype.Fast,
                null, null, null, null, Apningstider.Tom, null), default));

        Assert.False(await t.OpprettBesok(new NyttVetbesok(
            dyrId, null, null, new DateOnly(2026, 1, 1), null,
            "  ", null, null, false, null, null, null), default));
    }

    [Fact]
    public async Task Kommende_time_og_kontroll_havner_i_forfaller_snart()
    {
        var h = await _fixture.OpprettHusstand("Dashbord vet");
        await using var db = _fixture.LagContext(h);
        var t = Tjeneste(db, h);
        var dyrId = await NyttDyr(db, h);

        var idag = DateOnly.FromDateTime(DateTime.UtcNow);

        await t.OpprettBesok(new NyttVetbesok(
            dyrId, null, null, idag.AddDays(3), new TimeOnly(9, 30),
            "Vaksine", null, null, false, null, null, null), default);

        // Et gjennomfort besok med avtalt oppfolging. Timen selv er forbi,
        // men kontrollen skal fortsatt varsles.
        await t.OpprettBesok(new NyttVetbesok(
            dyrId, null, null, idag.AddDays(-10), null,
            "Sarstell", null, 1200, false, null,
            NesteKontrollDato: idag.AddDays(5), null), default);

        var dashbord = await new DashbordService(
            db, new HandlelisteService(db,
                new Husstandskontekst { HusstandId = h })).Hent(default);

        var fra = dashbord.Forfaller.Where(p => p.Kilde == Kilde.Vetbesok).ToList();

        Assert.Equal(2, fra.Count);

        // Sortert stigende - det som kommer forst star oeverst.
        Assert.Equal(idag.AddDays(3), fra[0].Dato);
        Assert.Contains("09:30", fra[0].Tekst);
        Assert.Contains("Kontroll", fra[1].Tekst);
    }
}
