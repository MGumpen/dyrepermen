using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Application.Services;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Fase 2. Tjenestene testes mot ekte database, siden det er der
/// eierskapssjekken og sorteringen faktisk avgjores.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class VektOgBehandlingTester
{
    private readonly DatabaseFixture _fixture;

    public VektOgBehandlingTester(DatabaseFixture fixture) => _fixture = fixture;

    private static async Task<int> NyttDyr(
        Dyrepermen.Infrastructure.Persistence.DyrepermenDbContext db, int husstand)
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

    private static DashbordService NyDashbord(
        Dyrepermen.Infrastructure.Persistence.DyrepermenDbContext db,
        int husstandId)
        => new(db, new HandlelisteService(
            db, new Husstandskontekst { HusstandId = husstandId }));

    [Fact]
    public async Task Vekt_lagres_i_gram_og_vises_i_synkende_datorekkefolge()
    {
        var h = await _fixture.OpprettHusstand("Vekt");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var tjeneste = new VektService(db);

        // 27,4 kg skal bli 27400 gram.
        await tjeneste.Registrer(
            new NyVekt(dyrId, 27.4m, new DateOnly(2026, 8, 1), null), default);
        await tjeneste.Registrer(
            new NyVekt(dyrId, 28.1m, new DateOnly(2026, 8, 9), null), default);

        var historikk = await tjeneste.HentFor(dyrId, default);

        Assert.Equal(2, historikk.Count);
        Assert.Equal(28100, historikk[0].VektGram);
        Assert.Equal(new DateOnly(2026, 8, 9), historikk[0].Dato);
        Assert.Equal(27400, historikk[1].VektGram);
    }

    [Fact]
    public async Task Vekt_kan_ikke_registreres_pa_annen_husstands_dyr()
    {
        // Query-filteret hindrer lesing. Denne testen dekker SKRIVING - en
        // POST med fremmed dyrId skal avvises. Se plan kapittel 15.
        var a = await _fixture.OpprettHusstand("Eier");
        var b = await _fixture.OpprettHusstand("Fremmed");

        int dyrId;
        await using (var eier = _fixture.LagContext(a))
        {
            dyrId = await NyttDyr(eier, a);
        }

        await using var fremmed = _fixture.LagContext(b);
        var tjeneste = new VektService(fremmed);

        var ok = await tjeneste.Registrer(
            new NyVekt(dyrId, 10m, new DateOnly(2026, 8, 1), null), default);

        Assert.False(ok);

        await using var eierIgjen = _fixture.LagContext(a);
        Assert.Empty(await eierIgjen.Vekt.Where(v => v.DyrId == dyrId).ToListAsync());
    }

    [Fact]
    public async Task Behandling_med_neste_dato_dukker_opp_pa_dashbordet()
    {
        var h = await _fixture.OpprettHusstand("Behandling");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var idag = DateOnly.FromDateTime(DateTime.UtcNow);
        var behandling = new BehandlingService(db);

        // Innenfor vinduet på 14 dager.
        await behandling.Registrer(new NyBehandling(
            dyrId, BehandlingType.Ormekur, "Milbemax",
            idag.AddDays(-90), idag.AddDays(5), null), default);

        // Forfalt.
        await behandling.Registrer(new NyBehandling(
            dyrId, BehandlingType.Vaksine, null,
            idag.AddDays(-400), idag.AddDays(-3), null), default);

        // Langt fram i tid - skal IKKE vises.
        await behandling.Registrer(new NyBehandling(
            dyrId, BehandlingType.Tannrens, null,
            idag, idag.AddDays(200), null), default);

        var dashbord = await NyDashbord(db, h).Hent(default);

        Assert.Equal(2, dashbord.Forfaller.Count);

        // Forfalte oeverst: sortert stigende pa dato gir eldste - altsa den
        // forfalte - forst.
        Assert.True(dashbord.Forfaller[0].ErForfalt(idag));
        Assert.False(dashbord.Forfaller[1].ErForfalt(idag));

        Assert.Contains("Milbemax", dashbord.Forfaller[1].Tekst);
    }

    [Fact]
    public async Task Dashbordet_viser_siste_vekt_per_dyr()
    {
        var h = await _fixture.OpprettHusstand("Siste vekt");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var vekt = new VektService(db);
        await vekt.Registrer(new NyVekt(dyrId, 27.4m, new DateOnly(2026, 8, 1), null), default);
        await vekt.Registrer(new NyVekt(dyrId, 28.1m, new DateOnly(2026, 8, 9), null), default);

        var dashbord = await NyDashbord(db, h).Hent(default);

        var kort = Assert.Single(dashbord.Dyr);
        Assert.Equal(28100, kort.SisteVektGram);
        Assert.Equal(new DateOnly(2026, 8, 9), kort.SisteVektDato);
    }
}
