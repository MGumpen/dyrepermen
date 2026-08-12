using System.Globalization;
using System.Reflection;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Application.Services;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Infrastructure;
using Dyrepermen.Web;
using Dyrepermen.Infrastructure.Persistence;
using Dyrepermen.Web.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var norsk = new CultureInfo("nb-NO");

// AutoValidateAntiforgeryToken globalt: antiforgery kreves pa ALLE usikre
// metoder (POST, PUT, PATCH, DELETE), ikke bare der noen husket attributtet.
//
// De 39 eksplisitte [ValidateAntiForgeryToken] star igjen med vilje. De sier
// hva handlingen krever, der handlingen er - men beskyttelsen hviler ikke
// lenger pa at nummer 40 far det samme.
builder.Services.AddControllersWithViews(o =>
    o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddHttpContextAccessor();

// Lokalt kommer denne fra "dotnet user-secrets", i produksjon fra
// miljovariabelen ConnectionStrings__Postgres. Aldri fra appsettings.
//
// Feil raskt og forstaelig. Uten sjekken kaster Npgsql lenger nede med
// "Host can't be null", og feilmeldingen sier ingenting om hva du skal gjore.
var tilkobling = builder.Configuration.GetConnectionString("Postgres");
if (string.IsNullOrWhiteSpace(tilkobling))
{
    throw new InvalidOperationException(
        "Tilkoblingsstrengen 'Postgres' mangler. Lokalt settes den med:\n\n" +
        "  dotnet user-secrets set \"ConnectionStrings:Postgres\" \\\n" +
        "    \"Host=localhost;Port=5434;Database=dyrepermen;" +
        "Username=dyrepermen;Password=utvikling;Maximum Pool Size=10\" \\\n" +
        "    --project src/Dyrepermen.Web\n\n" +
        "I produksjon settes miljovariabelen ConnectionStrings__Postgres.");
}

// Neon og Render oppgir databasen som URI. Npgsql tar ikke imot den formen
// og kaster i konstruktoren, for den har forsokt a kontakte noe - feilen ser
// derfor ut som et nettverksproblem uten a vaere det.
try
{
    tilkobling = Tilkoblingsstreng.Normaliser(tilkobling);
}
catch (Exception feil)
{
    // Verdien gjengis ALDRI i meldingen. Den inneholder passordet, og en
    // oppstartsfeil havner i utrullingsloggen der hvem som helst med tilgang
    // til Render leser den.
    throw new InvalidOperationException(
        "Tilkoblingsstrengen 'Postgres' lar seg ikke tolke. Den ma vaere "
        + "enten nokkel/verdi:\n\n"
        + "  Host=vert;Port=5432;Database=base;Username=bruker;Password=…\n\n"
        + "eller en URI slik Neon oppgir den:\n\n"
        + "  postgresql://bruker:passord@vert/base?sslmode=require\n\n"
        + "Verdien gjengis ikke her, fordi den inneholder passordet.", feil);
}

builder.Services.AddDbContext<DyrepermenDbContext>(opt =>
    opt.UseNpgsql(
           tilkobling,
           npg => npg.MigrationsAssembly("Dyrepermen.Infrastructure"))
       .UseSnakeCaseNamingConvention());

// Husstandskontekst registreres som seg selv OG bak grensesnittet, med samme
// instans per foresporsel. Middlewaren trenger den konkrete typen for a sette
// verdien; query-filtrene leser den gjennom grensesnittet. Se ADR 0001.
builder.Services.AddScoped<Husstandskontekst>();
builder.Services.AddScoped<IHusstandContext>(
    sp => sp.GetRequiredService<Husstandskontekst>());
builder.Services.AddScoped<IGjeldendeBruker>(
    sp => sp.GetRequiredService<Husstandskontekst>());

// Implementasjonene bak grensesnittene i Application. Se ADR 0007.
builder.Services.LeggTilInfrastruktur();

builder.Services.AddIdentity<Bruker, IdentityRole<int>>(o =>
{
    o.User.RequireUniqueEmail = true;
    o.SignIn.RequireConfirmedAccount = false;

    // Reglene kommer fra Passordkrav, og BARE derfra. Settes de her for
    // hand, sprikte de fra det skjemaet forteller brukeren - og det var
    // nettopp det som gjorde at et gyldig passord ble avvist med et krav
    // ingen hadde sett.
    //
    // De fire under ma settes eksplisitt. Identity har dem PA som standard,
    // sa a la dem sta betyr a kreve tall, sma bokstaver og spesialtegn i
    // stillhet.
    o.Password.RequiredLength = Passordkrav.MinLengde;

    // AV med vilje. Identity sin egen sjekk er ASCII-basert og godtar ikke
    // AE, OE og AA som store bokstaver. StorBokstavValidator under gjor
    // jobben i stedet, med char.IsUpper.
    o.Password.RequireUppercase = false;
    o.Password.RequireDigit = false;
    o.Password.RequireLowercase = false;
    o.Password.RequireNonAlphanumeric = false;
    o.Password.RequiredUniqueChars = 1;
    o.Lockout.MaxFailedAccessAttempts = 5;
    o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<DyrepermenDbContext>()
.AddPasswordValidator<StorBokstavValidator>()
.AddDefaultTokenProviders();

// Den viktigste enkeltdetaljen for at "husk meg" skal virke i praksis.
// Standard er filsystemet i containeren, og da forsvinner nokkelringen ved
// hver utrulling - alle blir logget ut. Se plan kapittel 11.2.
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<DyrepermenDbContext>()
    // Intern nokkelringidentifikator. Ma ALDRI endres - heller ikke ved
    // navnebytte. Endring ugyldiggjor alle innloggingskapsler.
    .SetApplicationName("dyrepermen")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// Sikker kapsel er standard. Eneste grunn til a sla den av er lokal
// container-kjoring over http (docker compose --profile full), der det ikke
// star noen TLS-terminator foran slik Render har. Med Always settes
// innloggingskapselen aldri over http, og innlogging virker ikke i det hele
// tatt - uten at noe sier fra om hvorfor.
//
// Valget er eksplisitt konfigurasjon, ikke en IsDevelopment()-sjekk, slik at
// standarden er sikker og avviket ma skrives ned et sted man ser det.
var krevSikkerKapsel =
    builder.Configuration.GetValue("Sikkerhet:KrevSikkerKapsel", true);

// Sikkerhetsnett: umulig a rulle ut med avslatt sikker kapsel.
if (!krevSikkerKapsel && builder.Environment.IsProduction())
{
    throw new InvalidOperationException(
        "Sikkerhet:KrevSikkerKapsel er slatt av i Production. Det sender "
        + "innloggingskapselen i klartekst. Fjern innstillingen.");
}

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/logg-inn";
    o.LogoutPath = "/logg-ut";
    // Ikke /logg-inn: en gjest som mangler rettigheter ER innlogget,
    // og a sende henne til innloggingssiden er misvisende.
    o.AccessDeniedPath = "/ingen-tilgang";

    o.ExpireTimeSpan = TimeSpan.FromDays(30);
    o.SlidingExpiration = true;

    o.Cookie.Name = "dyrepermen_auth";
    o.Cookie.HttpOnly = true;
    o.Cookie.SecurePolicy = krevSikkerKapsel
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.IsEssential = true;
});

// Hevet fra 30 minutter for a spare databasetreff pa et gratis databaseniva.
// Konsekvens: passordbytte logger ut andre enheter innen 12 timer, ikke straks.
builder.Services.Configure<SecurityStampValidatorOptions>(
    o => o.ValidationInterval = TimeSpan.FromHours(12));

// Fail closed: alt krever innlogging med mindre det er merket [AllowAnonymous].
// Glemmer man a sikre en ny controller, blir den last - ikke apen.
builder.Services.AddAuthorization(o =>
{
    o.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Fast norsk kultur, ikke forhandlet. RequestCultureProviders.Clear() er
// viktig: uten den leser ASP.NET Core Accept-Language, og en bruker med
// engelsk nettleser far punktum som desimalskilletegn mens skjemaet
// forventer komma. Se plan kapittel 7.3.
builder.Services.Configure<RequestLocalizationOptions>(o =>
{
    o.DefaultRequestCulture = new RequestCulture(norsk, norsk);
    o.SupportedCultures = [norsk];
    o.SupportedUICultures = [norsk];
    o.RequestCultureProviders.Clear();
});

// Render terminerer TLS i sin proxy og videresender som http. Uten dette ser
// applikasjonen en http-foresporsel, og to ting bryter samtidig:
// UseHttpsRedirection sender klienten i en evig omdirigeringslokke, og
// CookieSecurePolicy.Always nekter a sette innloggingskapselen - innlogging
// virker rett og slett ikke i produksjon.
// Proxyens IP er ukjent pa forhand, sa listene ma tommes.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

if (!krevSikkerKapsel)
{
    app.Logger.LogWarning(
        "Sikker innloggingskapsel er AVSLATT. Kun for lokal kjoring over http.");
}

// Ma sta forst i pipelinen, for noe leser skjema eller klient-IP.
app.UseForwardedHeaders();

// Rett etter, sa hodene folger ALLE svar - ogsa statiske filer og feilsider.
// Star den lenger ned, mangler de nettopp der de trengs mest.
app.UseMiddleware<Sikkerhetshoder>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // Noytral side uten stakksporing. En PostgresException som lekker til
    // brukeren kan avslore tabellnavn og constraint-navn.
    app.UseExceptionHandler("/feil");
    app.UseStatusCodePagesWithReExecute("/feil/{0}");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();

app.UseAuthentication();

// MA sta etter UseAuthentication og for alt som leser IHusstandContext.
// Rekkefolgen her er ikke kosmetisk: kjorer ikke middlewaren, star
// HusstandId pa 0 og alle query-filtre gir tomt resultatsett.
app.UseMiddleware<HusstandMiddleware>();

app.UseAuthorization();

// MapControllers, ikke MapControllerRoute. Alle controllere bruker
// attributtruting med norske ruter (plan kapittel 9), og en konvensjonell
// {controller}/{action}-rute ville aldri matchet noe - attributtrutede
// handlinger er utelatt fra konvensjonell ruting. Den ville bare ligget der
// og sett ut som om den gjorde noe.
//
// Bieffekten er ogsa onsket: en ny controller uten rutattributt blir
// utilgjengelig i stedet for a dukke opp pa en URL ingen har bestemt.
app.MapControllers();

// Brukes av Render som helsesjekk og av utrullingsjobben som roykttest.
// Skal IKKE treffe databasen - en helsesjekk som feiler fordi Neon sover,
// far Render til a restarte en frisk app. Se plan kapittel 9.1.
app.MapGet("/helse", () => Results.Ok(new
{
    status = "ok",
    versjon = Assembly.GetEntryAssembly()
        ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion ?? "ukjent",
    tid = DateTimeOffset.UtcNow
})).AllowAnonymous();

// Skjemaet legges inn ved oppstart, slik at Render alene er nok til a fa
// appen i lufta - ingen manuelle terminalkommandoer, ingen egen jobb.
// Se ADR 0010.
//
// Forutsetningen er EN instans. Kjorer to instanser oppstart samtidig,
// migrerer de samtidig, og databasen havner i en tilstand ingen har
// designet. Skaleres appen ut, ma dette flyttes ut av oppstarten igjen.
//
// MigrateAsync er idempotent: den leser __EFMigrationsHistory og kjorer
// bare det som mangler. En omstart uten nye migrasjoner gjor ingenting.
using (var oppstart = app.Services.CreateScope())
{
    var db = oppstart.ServiceProvider.GetRequiredService<DyrepermenDbContext>();
    app.Logger.LogInformation("Sjekker databaseskjema");
    await db.Database.MigrateAsync();
    app.Logger.LogInformation("Databaseskjema er a jour");
}

app.Run();

// Gjor Program synlig for WebApplicationFactory i testprosjektet. Med
// top-level statements er klassen ellers intern og generert, og
// WebApplicationFactory<Program> kompilerer ikke.
//
// Dette er den anbefalte maten fra ASP.NET-teamet, ikke en omvei: testene
// skal starte NOEYAKTIG denne oppstarten, med samme middleware og samme
// Identity-oppsett. En egen testoppstart ville testet noe annet enn det som
// kjorer i produksjon.
public partial class Program;
