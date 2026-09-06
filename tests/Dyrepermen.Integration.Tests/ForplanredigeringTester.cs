using System.Net;
using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Dyrepermen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// En liten justering skal endre planen som gjelder, ikke legge en ny rad i
/// historikken. Se ADR 0013.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class ForplanredigeringTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public ForplanredigeringTester(DatabaseFixture fixture) => _fixture = fixture;

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

    private static async Task<int> NyttDyr(DyrepermenDbContext db, int husstand)
    {
        var dyr = new Dyr
        {
            HusstandId = husstand,
            Navn = "Luna",
            Art = Art.Hund,
            Kjonn = Kjonn.Tispe,
            Fodselsdato = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-200)
        };
        db.Dyr.Add(dyr);
        await db.SaveChangesAsync();
        return dyr.Id;
    }

    /// <summary>
    /// Kjernen: etter en redigering finnes det fortsatt EN plan, og den har
    /// den nye verdien. Blir det to, er historikken full av rader ingen har
    /// bedt om.
    /// </summary>
    [Fact]
    public async Task Redigering_endrer_planen_og_lager_ingen_ny_rad()
    {
        var h = await _fixture.OpprettHusstand("Redigering");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var tjeneste = new ForplanService(db);

        await tjeneste.Opprett(new Forplaninnhold(
            dyrId, Formetode.Gram, null, 400, 2, "Tørrfôr", null), default);

        var forId = (await tjeneste.HentAktiv(dyrId, default))!.Id;

        Assert.True(await tjeneste.Oppdater(new Forplaninnhold(
            dyrId, Formetode.Gram, null, 450, 3, "Tørrfôr", "Litt mer"), default));

        var etter = await tjeneste.HentAktiv(dyrId, default);

        Assert.Equal(forId, etter!.Id);
        Assert.Equal(450, etter.GramPerDag);
        Assert.Equal(3, etter.AntallMaltider);
        Assert.Equal("Litt mer", etter.Notat);

        // Og ingen ekstra rad i historikken.
        Assert.Equal(1, await db.Forplan.CountAsync(f => f.DyrId == dyrId));
    }

    /// <summary>
    /// "Lagre som ny plan" skal fortsatt gjore det den alltid har gjort. De
    /// to knappene skal vaere to ting, ikke to navn pa det samme.
    /// </summary>
    [Fact]
    public async Task Ny_plan_legger_den_gamle_bort_som_historikk()
    {
        var h = await _fixture.OpprettHusstand("Erstatning");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var tjeneste = new ForplanService(db);

        await tjeneste.Opprett(new Forplaninnhold(
            dyrId, Formetode.Gram, null, 400, 2, null, null), default);
        await tjeneste.Opprett(new Forplaninnhold(
            dyrId, Formetode.Gram, null, 450, 2, null, null), default);

        Assert.Equal(2, await db.Forplan.CountAsync(f => f.DyrId == dyrId));
        Assert.Equal(1, await db.Forplan.CountAsync(f => f.DyrId == dyrId && f.Aktiv));
        Assert.Equal(450, (await tjeneste.HentAktiv(dyrId, default))!.GramPerDag);
    }

    /// <summary>
    /// Tabellen erstattes i sin helhet, og en ny rad pa samme maned som en
    /// gammel skal ikke stange mot ux_forplantrinn_alder. Det er nettopp den
    /// raden man endrer nar en mengde skal justeres.
    /// </summary>
    [Fact]
    public async Task Redigering_erstatter_hele_torrfortabellen()
    {
        var h = await _fixture.OpprettHusstand("Tabell");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var tjeneste = new ForplanService(db);

        await tjeneste.Opprett(new Forplaninnhold(
            dyrId, Formetode.Tabell, 50, null, 2, "Råfôr", null,
            VektdelAndelProsent: 70,
            Tabelltrinn: [new Alderstrinn(3, 160), new Alderstrinn(4, 180)]), default);

        // Samme maneder, nye mengder, og en rad faerre.
        Assert.True(await tjeneste.Oppdater(new Forplaninnhold(
            dyrId, Formetode.Tabell, 50, null, 2, "Råfôr", null,
            VektdelAndelProsent: 60,
            Tabelltrinn: [new Alderstrinn(3, 170)]), default));

        var etter = await tjeneste.HentAktiv(dyrId, default);

        Assert.Equal(60, etter!.VektdelAndelProsent);
        var trinn = Assert.Single(etter.Trinn);
        Assert.Equal(3, trinn.AlderMnd);
        Assert.Equal(170, trinn.GramPerDag);
        Assert.Equal(1, await db.Forplantrinn.CountAsync(t => t.ForplanId == etter.Id));
    }

    /// <summary>
    /// Bytter metoden, skal feltene fra den gamle nulles ut. Ellers ligger
    /// det igjen en andel pa en plan som ikke blander noe, og
    /// ck_forplan_verdi slar inn som en DbUpdateException.
    /// </summary>
    [Fact]
    public async Task Redigering_til_en_annen_metode_rydder_opp_etter_seg()
    {
        var h = await _fixture.OpprettHusstand("Metodebytte");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var tjeneste = new ForplanService(db);

        await tjeneste.Opprett(new Forplaninnhold(
            dyrId, Formetode.Tabell, 50, null, 2, null, null,
            VektdelAndelProsent: 70,
            Tabelltrinn: [new Alderstrinn(3, 160)]), default);

        Assert.True(await tjeneste.Oppdater(new Forplaninnhold(
            dyrId, Formetode.Gram, null, 400, 2, null, null), default));

        var etter = await tjeneste.HentAktiv(dyrId, default);

        Assert.Equal(Formetode.Gram, etter!.Metode);
        Assert.Null(etter.ProsentTidels);
        Assert.Null(etter.VektdelAndelProsent);
        Assert.Empty(etter.Trinn);

        // Trinnene skal vaere borte fra databasen, ikke bare fra visningen.
        Assert.Equal(0, await db.Forplantrinn.CountAsync(t => t.ForplanId == etter.Id));
    }

    [Fact]
    public async Task Uten_aktiv_plan_finnes_det_ingenting_a_redigere()
    {
        var h = await _fixture.OpprettHusstand("Tomt");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        Assert.False(await new ForplanService(db).Oppdater(new Forplaninnhold(
            dyrId, Formetode.Gram, null, 400, 2, null, null), default));
    }

    // ----- Gjennom skjemaet -------------------------------------------

    [Fact]
    public async Task Siden_tilbyr_bade_a_endre_og_a_lagre_som_ny()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);
        await Testoppsett.ForplanIGram(klient, dyrId, 400, 2);

        var html = WebUtility.HtmlDecode(
            await (await klient.Hent($"/dyr/{dyrId}/forplan"))
                .Content.ReadAsStringAsync());

        Assert.Contains("Lagre endringer", html);
        Assert.Contains("Lagre som ny plan", html);
        Assert.Contains($"/dyr/{dyrId}/forplan/rediger", html);
    }

    /// <summary>
    /// Opprettelsesdatoen ville ellers blitt staende over et innhold fra en
    /// helt annen dag.
    /// </summary>
    [Fact]
    public async Task Redigert_plan_viser_naar_den_sist_ble_endret()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);
        await Testoppsett.ForplanIGram(klient, dyrId, 400, 2);

        var svar = await klient.Post(
            $"/dyr/{dyrId}/forplan/rediger",
            new Dictionary<string, string>
            {
                ["Metode"] = ((int)Formetode.Gram).ToString(),
                ["GramPerDag"] = "450",
                ["AntallMaltider"] = "2"
            },
            tokenFra: $"/dyr/{dyrId}/forplan");

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Endringen ble ikke lagret: {await Skjemaklient.Feilmeldinger(svar)}");

        var html = WebUtility.HtmlDecode(
            await (await klient.Hent($"/dyr/{dyrId}/forplan"))
                .Content.ReadAsStringAsync());

        Assert.Contains("450 g", html);
        Assert.Contains("Sist endret", html);
    }

    /// <summary>
    /// Funksjonsbryteren styrer visning OG tilgang. En gammel faneside skal
    /// ikke kunne skrive til en avslatt funksjon - samme svar som for et dyr
    /// i en annen husstand.
    /// </summary>
    [Fact]
    public async Task Avslatt_forplan_avviser_ogsa_redigering()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);
        await Testoppsett.ForplanIGram(klient, dyrId, 400, 2);

        // Tokenet ma hentes for bryteren slas av - etterpa svarer siden 404.
        var token = await klient.HentToken($"/dyr/{dyrId}/forplan");

        var av = await klient.Post(
            $"/dyr/{dyrId}/rediger",
            new Dictionary<string, string>
            {
                ["Navn"] = "Luna",
                ["Art"] = ((int)Art.Hund).ToString(),
                ["Kjonn"] = ((int)Kjonn.Tispe).ToString(),
                ["ForingsloggAktiv"] = "false",
                ["ForplanAktiv"] = "false"
            },
            tokenFra: $"/dyr/{dyrId}/rediger");

        Assert.True(Skjemaklient.GikkGjennom(av));

        var svar = await klient.PostMedToken(
            $"/dyr/{dyrId}/forplan/rediger",
            new Dictionary<string, string>
            {
                ["Metode"] = ((int)Formetode.Gram).ToString(),
                ["GramPerDag"] = "450",
                ["AntallMaltider"] = "2"
            },
            token);

        Assert.Equal(HttpStatusCode.NotFound, svar.StatusCode);
    }
}
