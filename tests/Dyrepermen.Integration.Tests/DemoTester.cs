using System.Net;
using System.Reflection;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Abstractions;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Infrastructure.Persistence;
using Dyrepermen.Infrastructure.Services;
using Dyrepermen.Web.Controllers;
using Dyrepermen.Web.Filtre;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// DemoService mot ekte PostgreSQL. Her moter malen databasens regler for
/// forste gang - enhetstestene kan ikke se dem. Se ADR 0015.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class DemoTester : IAsyncLifetime
{
    private static readonly MethodInfo TellRader = typeof(DemoTester)
        .GetMethod(nameof(Antall), BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public DemoTester(DatabaseFixture fixture) => _fixture = fixture;

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

    [Fact]
    public async Task Start_legger_inn_hele_malen()
    {
        var bruker = await NyDemo();

        await using (var db = _fixture.LagContext(husstandId: 0))
        {
            var utloper = await db.Users
                .Where(u => u.Id == bruker)
                .Select(u => u.DemoUtloper)
                .SingleAsync();

            Assert.InRange(utloper!.Value,
                DateTimeOffset.UtcNow.AddHours(23), DateTimeOffset.UtcNow.AddHours(25));
        }

        await using var husstand = _fixture.LagContext(await HusstandTil(bruker));

        Assert.Equal(3, await husstand.Set<Dyr>().CountAsync());
        Assert.Equal(2, await husstand.Set<Veterinar>().CountAsync());
        Assert.Equal(2, await husstand.Set<Informasjon>().CountAsync());
        Assert.Equal(3, await husstand.Set<Handleliste>().CountAsync());
    }

    [Fact]
    public async Task Utlopt_demo_ryddes_uten_a_etterlate_rader()
    {
        var gammel = await NyDemo();
        var husstandId = await HusstandTil(gammel);

        await using (var db = _fixture.LagContext(husstandId: 0))
        {
            await db.Users
                .Where(u => u.Id == gammel)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    u => u.DemoUtloper, DateTimeOffset.UtcNow.AddMinutes(-1)));
        }

        // Neste demo rydder den utlopte.
        await NyDemo();

        await using var etter = _fixture.LagContext(husstandId);

        Assert.False(await etter.Users.AnyAsync(u => u.Id == gammel));
        Assert.False(await etter.Husstand.AnyAsync(h => h.Id == husstandId));

        // Alle husstandsbundne typer i modellen, slik FilterTester gar gjennom
        // dem. Query-filteret avgrenser tellingen til den gamle husstanden -
        // sa en ny tabell som ikke ryddes, far testen til a feile av seg selv.
        var igjen = new List<string>();
        foreach (var type in etter.Model.GetEntityTypes()
                     .Where(t => t.ClrType.IsAssignableTo(typeof(IHusstandsbundet))))
        {
            var antall = await (Task<int>)TellRader
                .MakeGenericMethod(type.ClrType)
                .Invoke(null, [etter])!;

            if (antall > 0)
            {
                igjen.Add($"{type.ClrType.Name}: {antall}");
            }
        }

        Assert.True(igjen.Count == 0, $"Ikke ryddet: {string.Join(", ", igjen)}");
    }

    [Fact]
    public async Task Taket_stopper_nye_demoer()
    {
        await using var db = _fixture.LagContext(husstandId: 0);
        var naa = DateTimeOffset.UtcNow;
        var aktive = await db.Users.CountAsync(u => u.DemoUtloper > naa);

        // Fyll opp til taket med demobrukere uten husstand. En hel mal per
        // stykk ville bare gjort testen treg.
        var fyll = Enumerable.Range(0, DemoService.Tak - aktive)
            .Select(_ =>
            {
                var epost = $"tak-{Guid.NewGuid():N}@demo.invalid";
                return new Bruker
                {
                    UserName = epost,
                    NormalizedUserName = epost.ToUpperInvariant(),
                    Email = epost,
                    NormalizedEmail = epost.ToUpperInvariant(),
                    Visningsnavn = "Fyll",
                    DemoUtloper = naa.AddHours(1)
                };
            })
            .ToList();

        db.Users.AddRange(fyll);
        await db.SaveChangesAsync();

        try
        {
            Assert.Null(await Start());
        }
        finally
        {
            // Taket er delt tilstand. Star fyllet igjen, far de andre testene
            // i klassen ingen demo.
            var ider = fyll.Select(b => b.Id).ToList();
            await db.Users.Where(u => ider.Contains(u.Id)).ExecuteDeleteAsync();
        }
    }

    // --- Over HTTP -----------------------------------------------------------

    [Fact]
    public async Task Demoknappen_logger_inn_i_en_fersk_demo()
    {
        var klient = new Skjemaklient(_app.LagKlient());

        var svar = await StartDemo(klient);

        Assert.Equal(HttpStatusCode.Found, svar.StatusCode);
        Assert.Equal("/", svar.Headers.Location?.ToString());

        // Ikke vedvarende: uten Expires forsvinner kapselen nar nettleseren
        // lukkes, i stedet for a gjelde i 30 dager.
        var kapsel = svar.Headers.GetValues("Set-Cookie")
            .Single(k => k.StartsWith("dyrepermen_auth", StringComparison.Ordinal));
        Assert.DoesNotContain("expires=", kapsel, StringComparison.OrdinalIgnoreCase);

        var oversikt = await (await klient.Hent("/")).Content.ReadAsStringAsync();

        Assert.Contains("Trixie", oversikt);
        Assert.Contains("Rose", oversikt);
        Assert.Contains("Milo", oversikt);
        Assert.Contains("forfalt", oversikt);
        Assert.Contains("Avslutt demo", oversikt);

        // Veterinarkortet lister bare steder med telefonnummer.
        Assert.Contains("Demoklinikken", oversikt);
    }

    [Fact]
    public async Task Demo_uten_antiforgery_token_avvises()
    {
        var svar = await _app.LagKlient().PostAsync(
            "/demo", new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.BadRequest, svar.StatusCode);
    }

    [Fact]
    public async Task To_demoer_ser_ikke_hverandres_dyr()
    {
        var a = new Skjemaklient(_app.LagKlient());
        var b = new Skjemaklient(_app.LagKlient());
        await StartDemo(a);
        await StartDemo(b);

        await using var db = _fixture.LagContext(await HusstandTil(await SisteDemo()));
        var bsDyr = await db.Set<Dyr>().Select(d => d.Id).FirstAsync();

        // Samme svar som et dyr som ikke finnes - ingen hint om at det finnes
        // et annet sted.
        Assert.Equal(HttpStatusCode.NotFound, (await a.Hent($"/dyr/{bsDyr}")).StatusCode);
    }

    [Fact]
    public async Task Demo_kan_ikke_legge_til_medlemmer()
    {
        // Den viktigste sperren. Ellers kan en anonym besokende legge en ekte
        // persons adresse inn i demohusstanden, og personen ser plutselig en
        // fremmed husstand i menyen sin.
        var klient = new Skjemaklient(_app.LagKlient());
        await StartDemo(klient);

        var svar = await klient.Post("/innstillinger/medlem", new Dictionary<string, string>
        {
            ["nyttMedlemEpost"] = "noen@example.test",
            ["rolle"] = "Gjest"
        }, tokenFra: "/innstillinger");

        Assert.Equal(HttpStatusCode.Found, svar.StatusCode);
        Assert.Contains("/ingen-tilgang", svar.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData(typeof(InnstillingController), nameof(InnstillingController.LeggTilMedlem))]
    [InlineData(typeof(InnstillingController), nameof(InnstillingController.AngreInvitasjon))]
    [InlineData(typeof(InnstillingController), nameof(InnstillingController.FjernMedlem))]
    [InlineData(typeof(KontaktController), nameof(KontaktController.Send))]
    [InlineData(typeof(MinKontoController), nameof(MinKontoController.Slett))]
    [InlineData(typeof(HusstandController), nameof(HusstandController.Opprett))]
    public void Handlingen_er_stengt_i_demo(Type controller, string handling)
    {
        Assert.NotNull(controller.GetMethod(handling)!
            .GetCustomAttribute<StengtIDemoAttribute>());
    }

    [Fact]
    public async Task Opprett_egen_konto_sletter_demoen_og_gar_til_registreringen()
    {
        var klient = new Skjemaklient(_app.LagKlient());
        await StartDemo(klient);
        var bruker = await SisteDemo();

        var svar = await klient.Post("/logg-ut", new Dictionary<string, string>
        {
            ["registrer"] = "true"
        }, tokenFra: "/");

        Assert.Equal("/registrer", svar.Headers.Location?.ToString());

        await using var db = _fixture.LagContext(husstandId: 0);
        Assert.False(await db.Users.AnyAsync(u => u.Id == bruker));
    }

    [Fact]
    public async Task Sjette_demo_i_timen_fra_samme_adresse_avvises()
    {
        HttpResponseMessage svar = null!;
        for (var i = 0; i < 6; i++)
        {
            svar = await StartDemo(new Skjemaklient(_app.LagKlient()));
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, svar.StatusCode);
    }

    private static Task<HttpResponseMessage> StartDemo(Skjemaklient klient)
        => klient.Post("/demo", new Dictionary<string, string>(), tokenFra: "/logg-inn");

    /// <summary>Den nyeste demobrukeren. Testene i samlingen kjorer etter hverandre.</summary>
    private async Task<int> SisteDemo()
    {
        await using var db = _fixture.LagContext(husstandId: 0);
        return await db.Users
            .Where(u => u.DemoUtloper != null)
            .OrderByDescending(u => u.Id)
            .Select(u => u.Id)
            .FirstAsync();
    }

    private async Task<int?> Start()
    {
        using var omfang = _app.Services.CreateScope();
        return await omfang.ServiceProvider
            .GetRequiredService<IDemoService>()
            .Start(CancellationToken.None);
    }

    private async Task<int> NyDemo()
        => await Start() ?? throw new InvalidOperationException("Taket pa aktive demoer er nadd.");

    private async Task<int> HusstandTil(int brukerId)
    {
        await using var db = _fixture.LagContext(husstandId: 0);
        return await db.Husstandsmedlemskap
            .Where(m => m.BrukerId == brukerId)
            .Select(m => m.HusstandId)
            .SingleAsync();
    }

    private static Task<int> Antall<T>(DyrepermenDbContext db) where T : class
        => db.Set<T>().CountAsync();
}
