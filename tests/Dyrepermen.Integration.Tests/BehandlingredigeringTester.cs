using System.Net;
using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Dyrepermen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// "Neste gang" er en avtale om framtiden, ikke et faktum om fortiden.
/// Sier veterinaeren at ormekuren kan vente, skal datoen kunne flyttes uten
/// at behandlingen som faktisk ble gitt ma slettes og legges inn pa nytt.
///
/// Handlingen sto i plan kapittel 9 hele tiden, men var aldri bygd.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class BehandlingredigeringTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public BehandlingredigeringTester(DatabaseFixture fixture) => _fixture = fixture;

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

    private static Behandlingsinnhold Ormekur(
        int dyrId, DateOnly dato, DateOnly? neste)
        => new(dyrId, BehandlingType.Ormekur, "Milbemax", dato, neste, null);

    /// <summary>
    /// Kjernen: datoen flyttes, behandlingen blir staende, og historikken
    /// far ikke en rad til.
    /// </summary>
    [Fact]
    public async Task Neste_gang_kan_flyttes_uten_a_slette_behandlingen()
    {
        var h = await _fixture.OpprettHusstand("Utsettelse");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var tjeneste = new BehandlingService(db);

        var gitt = new DateOnly(2026, 8, 1);
        await tjeneste.Registrer(Ormekur(dyrId, gitt, new DateOnly(2026, 11, 1)), default);

        var id = (await tjeneste.HentFor(dyrId, default)).Single().Id;

        Assert.True(await tjeneste.Oppdater(
            id, Ormekur(dyrId, gitt, new DateOnly(2027, 2, 1)), default));

        var etter = Assert.Single(await tjeneste.HentFor(dyrId, default));

        Assert.Equal(id, etter.Id);
        Assert.Equal(new DateOnly(2027, 2, 1), etter.NesteDato);

        // Behandlingen som faktisk ble gitt, star urort.
        Assert.Equal(gitt, etter.Dato);
        Assert.Equal("Milbemax", etter.Preparat);
    }

    /// <summary>
    /// Blir det ikke aktuelt likevel, skal paminnelsen kunne fjernes helt -
    /// uten at behandlingen forsvinner med den.
    /// </summary>
    [Fact]
    public async Task Neste_gang_kan_tommes_helt()
    {
        var h = await _fixture.OpprettHusstand("Tomming");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var tjeneste = new BehandlingService(db);

        var gitt = new DateOnly(2026, 8, 1);
        await tjeneste.Registrer(Ormekur(dyrId, gitt, new DateOnly(2026, 11, 1)), default);
        var id = (await tjeneste.HentFor(dyrId, default)).Single().Id;

        Assert.True(await tjeneste.Oppdater(id, Ormekur(dyrId, gitt, null), default));

        Assert.Null((await tjeneste.HentFor(dyrId, default)).Single().NesteDato);
    }

    /// <summary>
    /// Id-en alene er ikke nok. En behandling som horer til et annet dyr
    /// skal ikke kunne rettes gjennom dette dyrets rute.
    /// </summary>
    [Fact]
    public async Task Behandling_pa_et_annet_dyr_lar_seg_ikke_rette()
    {
        var h = await _fixture.OpprettHusstand("To dyr");
        await using var db = _fixture.LagContext(h);
        var luna = await NyttDyr(db, h);
        var milo = await NyttDyr(db, h, "Milo");
        var tjeneste = new BehandlingService(db);

        await tjeneste.Registrer(
            Ormekur(luna, new DateOnly(2026, 8, 1), null), default);
        var id = (await tjeneste.HentFor(luna, default)).Single().Id;

        Assert.False(await tjeneste.Oppdater(
            id, Ormekur(milo, new DateOnly(2026, 8, 1), null), default));
    }

    /// <summary>
    /// Query-filteret er autorisasjonen. En fremmed husstands behandling
    /// finnes ikke herfra, uansett hvilken id som sendes inn.
    /// </summary>
    [Fact]
    public async Task Behandling_i_en_annen_husstand_finnes_ikke()
    {
        var a = await _fixture.OpprettHusstand("Hjemme");
        var b = await _fixture.OpprettHusstand("Naboen");

        int id, dyrId;

        await using (var ctxA = _fixture.LagContext(a))
        {
            dyrId = await NyttDyr(ctxA, a);
            var tjeneste = new BehandlingService(ctxA);
            await tjeneste.Registrer(
                Ormekur(dyrId, new DateOnly(2026, 8, 1), null), default);
            id = (await tjeneste.HentFor(dyrId, default)).Single().Id;
        }

        await using var ctxB = _fixture.LagContext(b);
        Assert.False(await new BehandlingService(ctxB).Oppdater(
            id, Ormekur(dyrId, new DateOnly(2026, 8, 1), null), default));
    }

    // ----- Gjennom skjemaet -------------------------------------------

    private static async Task<(Skjemaklient klient, int dyrId)> DyrMedOrmekur(
        Appfabrikk app)
    {
        var klient = await Testoppsett.InnloggetKlient(app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var svar = await klient.Post(
            $"/dyr/{dyrId}/behandling",
            new Dictionary<string, string>
            {
                // Navnet, ikke tallet: skjemaet bygger listen med
                // t.ToString() som verdi.
                ["Type"] = nameof(BehandlingType.Ormekur),
                ["Preparat"] = "Milbemax",
                ["Dato"] = "2026-08-01",
                ["NesteDato"] = "2026-11-01"
            },
            tokenFra: $"/dyr/{dyrId}/behandling");

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Behandlingen ble ikke lagret: {await Skjemaklient.Feilmeldinger(svar)}");

        return (klient, dyrId);
    }

    private static async Task<string> Side(Skjemaklient klient, string sti)
        => WebUtility.HtmlDecode(
            await (await klient.Hent(sti)).Content.ReadAsStringAsync());

    [Fact]
    public async Task Historikken_tilbyr_a_redigere_hver_rad()
    {
        var (klient, dyrId) = await DyrMedOrmekur(_app);

        var html = await Side(klient, $"/dyr/{dyrId}/behandling");

        Assert.Contains("Rediger", html);
        Assert.Contains("rediger=", html);
    }

    /// <summary>
    /// Skjemaet ma sta ferdig utfylt. Et tomt skjema ville betydd at man
    /// skrev inn alt pa nytt - altsa akkurat det redigeringen skal slippe.
    /// </summary>
    [Fact]
    public async Task Redigermodus_fyller_skjemaet_fra_behandlingen()
    {
        var (klient, dyrId) = await DyrMedOrmekur(_app);
        var id = await ForsteId(klient, dyrId);

        var html = await Side(klient, $"/dyr/{dyrId}/behandling?rediger={id}");

        Assert.Contains("Endre behandling", html);
        Assert.Contains("value=\"Milbemax\"", html);
        Assert.Contains("value=\"2026-11-01\"", html);
        Assert.Contains("Lagre endringer", html);
        Assert.Contains("Avbryt", html);
    }

    [Fact]
    public async Task Ny_dato_lagres_og_vises_i_historikken()
    {
        var (klient, dyrId) = await DyrMedOrmekur(_app);
        var id = await ForsteId(klient, dyrId);

        var svar = await klient.Post(
            $"/dyr/{dyrId}/behandling/{id}/rediger",
            new Dictionary<string, string>
            {
                ["Type"] = nameof(BehandlingType.Ormekur),
                ["Preparat"] = "Milbemax",
                ["Dato"] = "2026-08-01",
                ["NesteDato"] = "2027-02-01",
                ["Notat"] = "Utsatt etter beskjed fra veterinær"
            },
            tokenFra: $"/dyr/{dyrId}/behandling?rediger={id}");

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Endringen ble ikke lagret: {await Skjemaklient.Feilmeldinger(svar)}");

        var html = await Side(klient, $"/dyr/{dyrId}/behandling");

        // nb-NO forkorter maneden med punktum: "1. feb. 2027".
        Assert.Contains("neste 1. feb. 2027", html);
        Assert.DoesNotContain("nov. 2026", html);
        Assert.Contains("Utsatt etter beskjed fra veterinær", html);
    }

    /// <summary>
    /// En lenke fra en fane som ble staende apen mens raden ble slettet et
    /// annet sted. Siden skal fortsatt virke, med et tomt skjema.
    /// </summary>
    [Fact]
    public async Task Ukjent_id_i_lenken_gir_et_tomt_skjema_og_ikke_en_feilside()
    {
        var (klient, dyrId) = await DyrMedOrmekur(_app);

        var svar = await klient.Hent($"/dyr/{dyrId}/behandling?rediger=999999");

        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);

        var html = WebUtility.HtmlDecode(await svar.Content.ReadAsStringAsync());
        Assert.Contains("Registrer behandling", html);
        Assert.DoesNotContain("Endre behandling", html);
    }

    /// <summary>Leser id-en ut av rediger-lenken i historikken.</summary>
    private static async Task<int> ForsteId(Skjemaklient klient, int dyrId)
    {
        var html = await Side(klient, $"/dyr/{dyrId}/behandling");
        var treff = System.Text.RegularExpressions.Regex.Match(
            html, @"rediger=(\d+)");

        Assert.True(treff.Success, "Fant ingen rediger-lenke i historikken.");
        return int.Parse(treff.Groups[1].Value);
    }
}
