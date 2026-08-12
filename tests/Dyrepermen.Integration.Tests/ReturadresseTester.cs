using System.Reflection;
using Dyrepermen.Web;
using Dyrepermen.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Hvor man lander etter et husstandsbytte.
///
/// Meldt inn: sto man pa et dyr i en husstand og byttet til en annen, kom det
/// en feilside - dyre-ID-en hang igjen i adressen, og dyret finnes ikke i den
/// nye husstanden.
/// </summary>
public sealed class ReturadresseTester
{
    [Theory]
    // Dyp sti mot en ressurs: ressursen slippes, seksjonen beholdes.
    [InlineData("/dyr/7", "/dyr")]
    [InlineData("/dyr/7/rediger", "/dyr")]
    [InlineData("/dyr/7/vekt", "/dyr")]
    [InlineData("/dyr/7/behandling", "/dyr")]
    [InlineData("/veterinar/1", "/veterinar")]
    [InlineData("/veterinar/time/3/rediger", "/veterinar")]
    // Seksjonen selv star urort.
    [InlineData("/handleliste", "/handleliste")]
    [InlineData("/informasjon", "/informasjon")]
    [InlineData("/innstillinger", "/innstillinger")]
    [InlineData("/", "/")]
    public void Stien_kuttes_til_seksjonen(string fra, string forventet)
        => Assert.Equal(forventet, Returadresse.Seksjon(fra));

    [Fact]
    public void Sporrestrengen_baerer_ikke_ID_en_videre()
    {
        // /forplan?dyrId=7 ville sluppet gjennom hvis stien ble delt opp for
        // sporrestrengen var fjernet.
        Assert.Equal("/", Returadresse.Seksjon("/forplan?dyrId=7"));
        Assert.Equal("/dyr", Returadresse.Seksjon("/dyr/7?fane=vekt"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/husstand/oppsett")]   // ingen forside a lande pa
    [InlineData("/feil/404")]
    [InlineData("/noe-som-ikke-finnes")]
    public void Ukjent_eller_tomt_gir_dashbordet(string? sti)
    {
        // Tillatelsesliste, ikke blokkeringsliste: en sti vi ikke kjenner
        // skal ende pa oversikten, ikke pa en 404.
        Assert.Equal("/", Returadresse.Seksjon(sti));
    }

    [Fact]
    public void Hver_seksjon_i_listen_har_faktisk_en_forside()
    {
        // Uten denne kan en oppforing bli staende etter at ruten er dopt om,
        // og da sender byttet brukeren rett i en 404 - altsa nayaktig den
        // feilen vi retter.
        var ruter = typeof(DyrController).Assembly
            .GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract)
            .Where(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance
                                     | BindingFlags.DeclaredOnly)
                         .Any(HarForside))
            .Select(t => t.GetCustomAttribute<RouteAttribute>()?.Template)
            .Where(r => r is not null)
            .Select(r => r!.ToLowerInvariant())
            .ToHashSet();

        var uten = Returadresse.Seksjoner
            .Where(s => !ruter.Contains(s))
            .ToList();

        Assert.True(
            uten.Count == 0,
            "Disse seksjonene star i Returadresse, men har ingen controller "
            + "med [Route] og en forside. Et bytte derfra ville endt i en "
            + "404:\n  " + string.Join("\n  ", uten));
    }

    /// <summary>En GET uten ekstra sti - altsa seksjonens forside.</summary>
    private static bool HarForside(MethodInfo m)
        => m.GetCustomAttributes<HttpGetAttribute>()
            .Any(a => string.IsNullOrEmpty(a.Template));
}
