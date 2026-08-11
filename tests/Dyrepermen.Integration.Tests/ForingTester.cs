using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Dyrepermen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Integration.Tests;

/// <summary>Akseptansekriteriene for fase 6b, plan kapittel 16.</summary>
[Collection(Databasesamling.Navn)]
public sealed class ForingTester
{
    private readonly DatabaseFixture _fixture;

    public ForingTester(DatabaseFixture fixture) => _fixture = fixture;

    private static async Task<int> NyttDyr(
        DyrepermenDbContext db, int husstand, bool foringPa)
    {
        var dyr = new Dyr
        {
            HusstandId = husstand,
            Navn = "Luna",
            Art = Art.Hund,
            Kjonn = Kjonn.Tispe,
            ForingsloggAktiv = foringPa
        };
        db.Dyr.Add(dyr);
        await db.SaveChangesAsync();
        return dyr.Id;
    }

    [Fact]
    public async Task Bryteren_av_blokkerer_registrering_pa_serveren()
    {
        // Kriteriet: bryteren skjuler fanen OG POST mot endepunktet avvises.
        // Uten sjekken i tjenesten kan en gammel faneside eller et bokmerke
        // skrive til en avslatt funksjon. Se plan kapittel 8.2.
        var h = await _fixture.OpprettHusstand("Bryter av");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h, foringPa: false);

        var tjeneste = new ForingService(db);

        Assert.False(await tjeneste.Registrer(
            new NyForing(dyrId, 200, null, null), default));

        Assert.Equal(0, await db.Foring.CountAsync());
    }

    [Fact]
    public async Task Bryteren_pa_tillater_registrering()
    {
        var h = await _fixture.OpprettHusstand("Bryter pa");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h, foringPa: true);

        Assert.True(await new ForingService(db).Registrer(
            new NyForing(dyrId, 200, "morgenmat", null), default));

        var rad = await db.Foring.SingleAsync();
        Assert.Equal(200, rad.MengdeGram);
        Assert.Equal("morgenmat", rad.Kommentar);
    }

    [Fact]
    public async Task Tidspunktet_settes_pa_serveren()
    {
        // Kriteriet: tidspunkt kan ikke sendes fra klient. NyForing har
        // ingen tidsparameter i det hele tatt - det er selve garantien.
        var h = await _fixture.OpprettHusstand("Servertid");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h, foringPa: true);

        var for_ = DateTimeOffset.UtcNow.AddSeconds(-2);
        await new ForingService(db).Registrer(
            new NyForing(dyrId, null, null, null), default);
        var etter = DateTimeOffset.UtcNow.AddSeconds(2);

        var rad = await db.Foring.SingleAsync();

        Assert.InRange(rad.Tidspunkt, for_, etter);
    }

    [Fact]
    public async Task Mengde_er_valgfri()
    {
        // Man skal kunne huke av at det er gjort, uten a oppgi mengde.
        var h = await _fixture.OpprettHusstand("Uten mengde");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h, foringPa: true);

        Assert.True(await new ForingService(db).Registrer(
            new NyForing(dyrId, null, null, null), default));

        Assert.Null((await db.Foring.SingleAsync()).MengdeGram);
    }

    [Fact]
    public async Task Tidspunktet_kan_rettes_i_etterkant()
    {
        // Glemmer man a huke av til man kommer hjem om kvelden, er
        // automatikken feil og ma kunne overstyres.
        var h = await _fixture.OpprettHusstand("Retting");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h, foringPa: true);

        var tjeneste = new ForingService(db);
        await tjeneste.Registrer(new NyForing(dyrId, null, null, null), default);

        var id = (await db.Foring.SingleAsync()).Id;
        var riktig = new DateTimeOffset(2026, 7, 4, 5, 12, 0, TimeSpan.Zero);

        Assert.True(await tjeneste.RedigerTid(dyrId, id, riktig, default));

        db.ChangeTracker.Clear();
        Assert.Equal(riktig, (await db.Foring.SingleAsync()).Tidspunkt);
    }

    [Fact]
    public async Task Retting_blokkeres_ogsa_nar_bryteren_er_av()
    {
        var h = await _fixture.OpprettHusstand("Retting blokkert");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h, foringPa: true);

        var tjeneste = new ForingService(db);
        await tjeneste.Registrer(new NyForing(dyrId, null, null, null), default);
        var id = (await db.Foring.SingleAsync()).Id;

        // Bryteren slas av etterpa.
        var dyr = await db.Dyr.SingleAsync(d => d.Id == dyrId);
        dyr.ForingsloggAktiv = false;
        await db.SaveChangesAsync();

        Assert.False(await tjeneste.RedigerTid(
            dyrId, id, DateTimeOffset.UtcNow, default));

        // Radene slettes ikke nar bryteren slas av - kun skjules.
        // Slas den pa igjen, er historikken der.
        Assert.Equal(1, await db.Foring.IgnoreQueryFilters()
            .CountAsync(f => f.DyrId == dyrId));
    }

    [Fact]
    public async Task Foring_pa_annen_husstands_dyr_avvises()
    {
        var a = await _fixture.OpprettHusstand("Eier foring");
        var b = await _fixture.OpprettHusstand("Fremmed foring");

        int dyrId;
        await using (var eier = _fixture.LagContext(a))
        {
            dyrId = await NyttDyr(eier, a, foringPa: true);
        }

        await using var fremmed = _fixture.LagContext(b);

        Assert.False(await new ForingService(fremmed).Registrer(
            new NyForing(dyrId, 200, null, null), default));
    }

    [Fact]
    public async Task Dashbordet_viser_sist_matet_kun_nar_bryteren_er_pa()
    {
        var h = await _fixture.OpprettHusstand("Sist matet");
        await using var db = _fixture.LagContext(h);

        var med = await NyttDyr(db, h, foringPa: true);
        var uten = new Dyr
        {
            HusstandId = h,
            Navn = "Uten logg",
            Art = Art.Katt,
            Kjonn = Kjonn.Hann,
            ForingsloggAktiv = false
        };
        db.Dyr.Add(uten);
        await db.SaveChangesAsync();

        var foring = new ForingService(db);
        await foring.Registrer(new NyForing(med, 200, null, null), default);

        // Raden finnes for begge - men kortet skal kun vise den for dyret
        // med bryteren pa.
        db.Foring.Add(new Foring { DyrId = uten.Id, Tidspunkt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var dashbord = await new DashbordService(
            db,
            new HandlelisteService(db, new Dyrepermen.Application.Services
                .Husstandskontekst { HusstandId = h })).Hent(default);

        Assert.NotNull(dashbord.Dyr.Single(d => d.Id == med).SistMatet);
        Assert.Null(dashbord.Dyr.Single(d => d.Id == uten.Id).SistMatet);
    }
}
