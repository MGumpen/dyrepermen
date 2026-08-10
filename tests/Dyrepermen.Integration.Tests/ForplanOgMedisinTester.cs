using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Dyrepermen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dyrepermen.Integration.Tests;

/// <summary>Akseptansekriteriene for fase 3, plan kapittel 16.</summary>
[Collection(Databasesamling.Navn)]
public sealed class ForplanOgMedisinTester
{
    private readonly DatabaseFixture _fixture;

    public ForplanOgMedisinTester(DatabaseFixture fixture) => _fixture = fixture;

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

    private static MedisinService NyMedisinTjeneste(DyrepermenDbContext db)
        => new(db, NullLogger<MedisinService>.Instance);

    [Fact]
    public async Task Prosentplan_gir_riktig_gram_ved_kjent_vekt()
    {
        var h = await _fixture.OpprettHusstand("Prosent");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        // 27,4 kg og 5,0 % skal gi 1370 gram per dag.
        db.Vekt.Add(new Vekt
        {
            DyrId = dyrId,
            VektGram = 27400,
            Dato = new DateOnly(2026, 8, 1)
        });
        await db.SaveChangesAsync();

        var tjeneste = new ForplanService(db);
        await tjeneste.Opprett(new NyForplan(
            dyrId, Formetode.Prosent, 50, null, 2, "Råfôr", null), default);

        var r = await tjeneste.BeregnAktiv(dyrId, default);

        Assert.True(r.HarPlan);
        Assert.False(r.ManglerVekt);
        Assert.Equal(1370, r.GramPerDag);

        // Vektgrunnlaget skal alltid folge med resultatet.
        Assert.Equal(27400, r.GrunnlagVektGram);
        Assert.Equal(new DateOnly(2026, 8, 1), r.GrunnlagDato);
    }

    [Fact]
    public async Task Prosentplan_uten_vekt_gir_ManglerVekt_og_ikke_null_gram()
    {
        var h = await _fixture.OpprettHusstand("Uten vekt");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var tjeneste = new ForplanService(db);
        await tjeneste.Opprett(new NyForplan(
            dyrId, Formetode.Prosent, 50, null, 2, null, null), default);

        var r = await tjeneste.BeregnAktiv(dyrId, default);

        // Kriteriet: "Registrer en vekt", ikke 0 gram. HarPlan skiller denne
        // tilstanden fra "ingen plan lagt inn".
        Assert.True(r.HarPlan);
        Assert.True(r.ManglerVekt);
    }

    [Fact]
    public async Task Prosentplan_folger_nyeste_vekt()
    {
        // Prosentmetoden er levende - mengden skal folge valpen oppover.
        var h = await _fixture.OpprettHusstand("Levende");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var tjeneste = new ForplanService(db);
        await tjeneste.Opprett(new NyForplan(
            dyrId, Formetode.Prosent, 50, null, 2, null, null), default);

        db.Vekt.Add(new Vekt { DyrId = dyrId, VektGram = 10000, Dato = new DateOnly(2026, 7, 1) });
        await db.SaveChangesAsync();
        Assert.Equal(500, (await tjeneste.BeregnAktiv(dyrId, default)).GramPerDag);

        db.Vekt.Add(new Vekt { DyrId = dyrId, VektGram = 12000, Dato = new DateOnly(2026, 8, 1) });
        await db.SaveChangesAsync();
        Assert.Equal(600, (await tjeneste.BeregnAktiv(dyrId, default)).GramPerDag);
    }

    [Fact]
    public async Task Ny_plan_erstatter_den_gamle_uten_a_bryte_unikhetsindeksen()
    {
        var h = await _fixture.OpprettHusstand("Erstatt");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var tjeneste = new ForplanService(db);
        await tjeneste.Opprett(new NyForplan(
            dyrId, Formetode.Gram, null, 400, 2, null, null), default);
        await tjeneste.Opprett(new NyForplan(
            dyrId, Formetode.Gram, null, 500, 3, null, null), default);

        var aktiv = await tjeneste.HentAktiv(dyrId, default);
        Assert.Equal(500, aktiv!.GramPerDag);

        // Den gamle beholdes som historikk.
        var alle = await db.Forplan.IgnoreQueryFilters()
            .Where(f => f.DyrId == dyrId).ToListAsync();
        Assert.Equal(2, alle.Count);
        Assert.Single(alle, f => f.Aktiv);
    }

    [Fact]
    public async Task Dose_for_tidlig_gir_advarsel_og_logges_ikke()
    {
        var h = await _fixture.OpprettHusstand("Dobbeltdose");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var tjeneste = NyMedisinTjeneste(db);
        await tjeneste.Registrer(new NyMedisin(
            dyrId, "Metacam", "1 ml", 12,
            DateOnly.FromDateTime(DateTime.UtcNow), null), default);

        var medisinId = await db.Medisin.Select(m => m.Id).SingleAsync();

        Assert.True((await tjeneste.LoggDose(dyrId, medisinId, null, false, default)).Ok);

        // Andre dose umiddelbart etter - 12 timers intervall er ikke passert.
        var nummerTo = await tjeneste.LoggDose(dyrId, medisinId, null, false, default);

        Assert.False(nummerTo.Ok);
        Assert.True(nummerTo.KreverBekreftelse);
        Assert.Contains("Neste dose kan tidligst gis", nummerTo.Melding);

        // Ingenting skal vaere lagret av forsoket.
        Assert.Equal(1, await db.Dose.CountAsync());
    }

    [Fact]
    public async Task Dose_kan_gis_likevel_nar_brukeren_bekrefter()
    {
        var h = await _fixture.OpprettHusstand("Bekreft");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var tjeneste = NyMedisinTjeneste(db);
        await tjeneste.Registrer(new NyMedisin(
            dyrId, "Metacam", "1 ml", 12,
            DateOnly.FromDateTime(DateTime.UtcNow), null), default);

        var medisinId = await db.Medisin.Select(m => m.Id).SingleAsync();

        await tjeneste.LoggDose(dyrId, medisinId, null, false, default);
        var bekreftet = await tjeneste.LoggDose(dyrId, medisinId, null, true, default);

        Assert.True(bekreftet.Ok);
        Assert.Equal(2, await db.Dose.CountAsync());
    }

    [Fact]
    public async Task Medisin_ved_behov_har_ingen_dobbeltdoseringssjekk()
    {
        // Intervall 0 betyr ved behov. Da finnes ingen "for tidlig".
        var h = await _fixture.OpprettHusstand("Ved behov");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var tjeneste = NyMedisinTjeneste(db);
        await tjeneste.Registrer(new NyMedisin(
            dyrId, "Smertestillende", "1 tablett", 0,
            DateOnly.FromDateTime(DateTime.UtcNow), null), default);

        var medisinId = await db.Medisin.Select(m => m.Id).SingleAsync();

        Assert.True((await tjeneste.LoggDose(dyrId, medisinId, null, false, default)).Ok);
        Assert.True((await tjeneste.LoggDose(dyrId, medisinId, null, false, default)).Ok);

        Assert.Equal(2, await db.Dose.CountAsync());
    }

    [Fact]
    public async Task Medisin_pa_annen_husstands_dyr_avvises()
    {
        var a = await _fixture.OpprettHusstand("Eier medisin");
        var b = await _fixture.OpprettHusstand("Fremmed medisin");

        int dyrId;
        await using (var eier = _fixture.LagContext(a))
        {
            dyrId = await NyttDyr(eier, a);
        }

        await using var fremmed = _fixture.LagContext(b);

        Assert.False(await NyMedisinTjeneste(fremmed).Registrer(
            new NyMedisin(dyrId, "X", "1", 0,
                DateOnly.FromDateTime(DateTime.UtcNow), null), default));

        Assert.False(await new ForplanService(fremmed).Opprett(
            new NyForplan(dyrId, Formetode.Gram, null, 400, 2, null, null), default));
    }
}
