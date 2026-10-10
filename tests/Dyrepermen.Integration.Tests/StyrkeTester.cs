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
/// Styrken pa en behandling: fritekst som lagres, vises og folger med nar
/// en paminnelse krysses av - men som ikke er en del av hva som folger opp
/// en paminnelse. Se ADR 0020.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed partial class StyrkeTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public StyrkeTester(DatabaseFixture fixture) => _fixture = fixture;

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

    [GeneratedRegex("""rediger=(\d+)#skjema""")]
    private static partial Regex Redigerlenke();

    private static async Task<string> Side(Skjemaklient klient, string sti)
        => WebUtility.HtmlDecode(
            await (await klient.Hent(sti)).Content.ReadAsStringAsync());

    private static Dictionary<string, string> Skjema(string styrke) => new()
    {
        // Navnet, ikke tallet. Nedtrekkslisten skriver ut enumnavnet.
        ["Type"] = BehandlingType.Ormekur.ToString(),
        ["Preparat"] = "Milbemax",
        ["Styrke"] = styrke,
        ["Dato"] = Idag.ToString("yyyy-MM-dd")
    };

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

    private static Task<Dashbord> Dashbord(DyrepermenDbContext db, int husstand)
        => new DashbordService(
            db, new HandlelisteService(db,
                new Husstandskontekst { HusstandId = husstand })).Hent(default);

    [Fact]
    public async Task Styrken_lagres_vises_og_kan_rettes()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);
        var side = $"/dyr/{dyrId}/behandling";

        var lagret = await klient.Post(side, Skjema("2,5 mg/25 mg"));
        Assert.True(
            Skjemaklient.GikkGjennom(lagret),
            $"Behandlingen ble ikke lagret: {await Skjemaklient.Feilmeldinger(lagret)}");

        var historikk = await Side(klient, side);
        Assert.Contains("Ormekur – Milbemax 2,5 mg/25 mg", historikk);

        // Endringsmodus fyller feltet, og en ny styrke lagres.
        var id = Redigerlenke().Match(historikk).Groups[1].Value;
        var skjema = await Side(klient, $"{side}?rediger={id}");
        Assert.Contains("value=\"2,5 mg/25 mg\"", skjema);

        var rettet = await klient.Post(
            $"{side}/{id}/rediger", Skjema("12,5 mg/125 mg"), tokenFra: $"{side}?rediger={id}");
        Assert.True(Skjemaklient.GikkGjennom(rettet),
            $"Endringen ble ikke lagret: {await Skjemaklient.Feilmeldinger(rettet)}");

        Assert.Contains("Ormekur – Milbemax 12,5 mg/125 mg", await Side(klient, side));
    }

    [Fact]
    public async Task For_lang_styrke_avvises()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var svar = await klient.Post(
            $"/dyr/{dyrId}/behandling", Skjema(new string('x', 41)));

        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        Assert.Contains("Styrken kan være høyst 40 tegn.",
            await Skjemaklient.Feilmeldinger(svar));
    }

    [Fact]
    public async Task Gitt_tar_med_styrken_og_paminnelsen_viser_den()
    {
        var h = await _fixture.OpprettHusstand("Styrke gitt");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var forrige = new Behandling
        {
            DyrId = dyrId,
            Type = BehandlingType.Ormekur,
            Preparat = "Milbemax",
            Styrke = "12,5 mg/125 mg",
            Dato = Idag.AddMonths(-3),
            NesteDato = Idag.AddDays(3)
        };
        db.Behandling.Add(forrige);
        await db.SaveChangesAsync();

        var paminnelse = Assert.Single((await Dashbord(db, h)).Forfaller);
        Assert.Equal("Ormekur – Milbemax 12,5 mg/125 mg", paminnelse.Tekst);

        var resultat = await new BehandlingService(db).Gitt(
            dyrId, forrige.Id, null, null, default);
        Assert.Equal("Ormekur – Milbemax 12,5 mg/125 mg", resultat.Beskrivelse);

        var ny = await db.Behandling.AsNoTracking()
            .SingleAsync(b => b.DyrId == dyrId && b.Id != forrige.Id);
        Assert.Equal("12,5 mg/125 mg", ny.Styrke);
    }

    /// <summary>
    /// Valpen vokser og far sterkere tablett. Det er fortsatt samme ormekur,
    /// og paminnelsen om den forrige skal forsvinne.
    /// </summary>
    [Fact]
    public async Task Ny_styrke_folger_opp_paminnelsen()
    {
        var h = await _fixture.OpprettHusstand("Styrke vokser");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        db.Behandling.Add(new Behandling
        {
            DyrId = dyrId,
            Type = BehandlingType.Ormekur,
            Preparat = "Milbemax",
            Styrke = "2,5 mg/25 mg",
            Dato = Idag.AddMonths(-1),
            NesteDato = Idag
        });
        await db.SaveChangesAsync();

        await new BehandlingService(db).Registrer(new Behandlingsinnhold(
            dyrId, BehandlingType.Ormekur, "Milbemax", Idag, null, null,
            Styrke: "12,5 mg/125 mg"), default);

        Assert.DoesNotContain((await Dashbord(db, h)).Forfaller,
            p => p.Kilde == Kilde.Behandling);
    }
}
