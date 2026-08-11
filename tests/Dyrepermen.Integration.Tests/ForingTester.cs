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

    private static async Task<Dashbord> HentDashbord(
        DyrepermenDbContext db, int husstand)
        => await new DashbordService(
            db,
            new HandlelisteService(db, new Dyrepermen.Application.Services
                .Husstandskontekst { HusstandId = husstand })).Hent(default);

    [Fact]
    public async Task Maltidstelleren_teller_kun_dagens_foringer()
    {
        // Uten datogrensen hadde dashbordet sagt "maltid 9 av 3" etter en uke,
        // og tallet ville aldri blitt riktig igjen.
        var h = await _fixture.OpprettHusstand("Teller i dag");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h, foringPa: true);

        db.Foring.Add(new Foring
        {
            DyrId = dyrId,
            Tidspunkt = DateTimeOffset.UtcNow.AddDays(-3)
        });
        await db.SaveChangesAsync();

        await new ForingService(db).Registrer(
            new NyForing(dyrId, 100, null, null), default);

        var kort = (await HentDashbord(db, h)).Dyr.Single();

        Assert.Equal(1, kort.MaltiderIDag);
    }

    [Fact]
    public async Task Kortet_viser_porsjonen_til_ETT_maltid_ikke_hele_dagen()
    {
        // 300 g pa tre maltider er 100 g i skala - ikke 300. Blandes de to,
        // far dyret tre ganger for mye.
        var h = await _fixture.OpprettHusstand("Porsjon");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h, foringPa: true);

        db.Forplan.Add(new Forplan
        {
            DyrId = dyrId,
            Metode = Formetode.Gram,
            GramPerDag = 300,
            AntallMaltider = 3,
            Aktiv = true,
            OpprettetDato = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        await db.SaveChangesAsync();

        var kort = (await HentDashbord(db, h)).Dyr.Single();

        Assert.Equal(100, kort.PorsjonGram);
        Assert.Equal(3, kort.AntallMaltider);
        Assert.Equal(1, kort.NesteMaltid);
        Assert.False(kort.AlleMaltiderGitt);
    }

    [Fact]
    public async Task Alle_maltider_gitt_slar_inn_pa_siste_porsjon()
    {
        var h = await _fixture.OpprettHusstand("Ferdig i dag");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h, foringPa: true);

        db.Forplan.Add(new Forplan
        {
            DyrId = dyrId,
            Metode = Formetode.Gram,
            GramPerDag = 200,
            AntallMaltider = 2,
            Aktiv = true,
            OpprettetDato = DateOnly.FromDateTime(DateTime.UtcNow)
        });
        await db.SaveChangesAsync();

        var tjeneste = new ForingService(db);
        await tjeneste.Registrer(new NyForing(dyrId, 100, null, null), default);

        Assert.False((await HentDashbord(db, h)).Dyr.Single().AlleMaltiderGitt);

        db.ChangeTracker.Clear();
        await tjeneste.Registrer(new NyForing(dyrId, 100, null, null), default);

        var kort = (await HentDashbord(db, h)).Dyr.Single();

        Assert.True(kort.AlleMaltiderGitt);

        // Knappen forsvinner ikke: en ekstra porsjon skal fortsatt kunne
        // registreres, og da ma tallet vaere der.
        Assert.Equal(100, kort.PorsjonGram);
    }

    [Fact]
    public async Task Godbit_teller_ikke_som_maltid()
    {
        // Selve grunnen til at typen finnes. Uten skillet ville dashbordet
        // sagt "maltid 3 av 3" fordi noen ga hunden en ostebit, og den som
        // kommer hjem vet ikke om middagen er gitt.
        var h = await _fixture.OpprettHusstand("Godbit teller ikke");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h, foringPa: true);

        var tjeneste = new ForingService(db);
        await tjeneste.Registrer(new NyForing(dyrId, 100, null, null), default);
        await tjeneste.Registrer(
            new NyForing(dyrId, 10, null, null, Foringstype.Godbit, "Tyggebein"),
            default);

        var kort = (await HentDashbord(db, h)).Dyr.Single();

        Assert.Equal(1, kort.MaltiderIDag);
        Assert.Equal(1, kort.GodbiterIDag);
    }

    [Fact]
    public async Task Godbit_avvises_nar_husstandsbryteren_er_av()
    {
        // Bryteren skjuler knappen OG stenger endepunktet. Uten sjekken i
        // tjenesten kan et bokmerke skrive til en avslatt funksjon.
        // Se plan kapittel 8.2.
        var h = await _fixture.OpprettHusstand("Godbit av");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h, foringPa: true);

        db.HusstandInnstilling.Add(new HusstandInnstilling
        {
            HusstandId = h,
            GodbitloggAktiv = false
        });
        await db.SaveChangesAsync();

        var tjeneste = new ForingService(db);

        Assert.False(await tjeneste.Registrer(
            new NyForing(dyrId, 10, null, null, Foringstype.Godbit, "Ost"),
            default));

        // Maltider skal fortsatt ga gjennom - bryteren gjelder kun godbiter.
        Assert.True(await tjeneste.Registrer(
            new NyForing(dyrId, 100, null, null), default));

        Assert.Equal(1, await db.Foring.CountAsync());
    }

    [Fact]
    public async Task Bryteren_lar_seg_faktisk_skru_av()
    {
        // EF utelater en verdi som er lik CLR-standarden nar egenskapen har
        // lagringsstandard. For en bool er CLR-standarden false - og med
        // HasDefaultValue(true) ville raden blitt lagret som PA uansett.
        // Denne testen ville feilet for konfigurasjonen ble rettet.
        var h = await _fixture.OpprettHusstand("Bryter av ved innsetting");
        await using var db = _fixture.LagContext(h);

        db.HusstandInnstilling.Add(new HusstandInnstilling
        {
            HusstandId = h,
            GodbitloggAktiv = false,
            VarslerAktiv = false
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var lagret = await db.HusstandInnstilling.SingleAsync();

        Assert.False(lagret.GodbitloggAktiv);
        Assert.False(lagret.VarslerAktiv);
    }

    [Fact]
    public async Task Fornavn_foreslas_fra_husstandens_egne_rader()
    {
        var a = await _fixture.OpprettHusstand("Forslag egne");
        var b = await _fixture.OpprettHusstand("Forslag fremmed");

        await using (var eier = _fixture.LagContext(a))
        {
            var dyrId = await NyttDyr(eier, a, foringPa: true);
            await new ForingService(eier).Registrer(
                new NyForing(dyrId, 100, null, null,
                    Foringstype.Maltid, "Royal Canin Maxi"),
                default);
        }

        await using var fremmed = _fixture.LagContext(b);

        // Query-filteret gjor jobben: en annen husstands foernavn skal ikke
        // lekke inn i forslagslisten.
        Assert.Empty(await new ForingService(fremmed)
            .HentFornavn(Foringstype.Maltid, default));

        await using var eget = _fixture.LagContext(a);

        Assert.Equal(
            ["Royal Canin Maxi"],
            await new ForingService(eget).HentFornavn(Foringstype.Maltid, default));

        // Godbiter og maltider har hver sin liste. "Tyggebein" hoerer ikke
        // hjemme blant forslagene til middag.
        Assert.Empty(await new ForingService(eget)
            .HentFornavn(Foringstype.Godbit, default));
    }

    [Fact]
    public async Task Uten_foringslogg_telles_ingen_maltider()
    {
        // Er loggen av, ville "0 av 2" lest som et etterslep for et dyr som
        // aldri skulle vaert foringsloggfort.
        var h = await _fixture.OpprettHusstand("Logg av");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h, foringPa: false);

        db.Foring.Add(new Foring { DyrId = dyrId, Tidspunkt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        Assert.Equal(0, (await HentDashbord(db, h)).Dyr.Single().MaltiderIDag);
    }
}
