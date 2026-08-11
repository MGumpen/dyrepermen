using System.Reflection;
using Dyrepermen.Web.Controllers;
using Dyrepermen.Web.Filtre;
using Microsoft.AspNetCore.Mvc;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Fail-closed sjekk av rolletilgang, i samme and som filterproven i
/// kapittel 17.3.
///
/// Attributtet [KreverEier] beskytter kun handlinger noen husker a merke.
/// Denne testen gar gjennom ALLE POST-handlinger og feiler hvis en mangler
/// enten attributtet eller en plass pa gjestelisten under. Da kan ingen
/// legge til en skrivehandling og glemme tilgangskontrollen - de ma ta
/// stilling til den for testen blir gronn igjen.
/// </summary>
public sealed class RolleTester
{
    /// <summary>
    /// Handlinger en gjest med vilje SKAL kunne utfore: logging av det
    /// daglige. Passer du hunden, ma du kunne notere at du ga mat og
    /// medisin - ellers far loggen hull nettopp de dagene noen andre hadde
    /// ansvaret. Se ADR 0009.
    /// </summary>
    private static readonly HashSet<string> ApentForGjest =
    [
        // Konto og innlogging - ikke husstandsdata i det hele tatt
        $"{nameof(KontoController)}.LoggInn",
        $"{nameof(KontoController)}.Registrer",
        $"{nameof(KontoController)}.LoggUt",
        $"{nameof(MinKontoController)}.Slett",
        $"{nameof(HusstandController)}.Opprett",
        $"{nameof(HusstandController)}.Bytt",

        // Daglig logging. Foringen er selve grunnen til at gjesterollen
        // finnes: passer du hunden, ma du kunne notere at du ga mat.
        $"{nameof(VektController)}.Registrer",
        $"{nameof(MedisinController)}.LoggDose",
        $"{nameof(ForingController)}.Registrer",

        // Samme to handlinger, men utfort fra dashbordet. Gjor de tynne
        // dashbordvariantene noe annet enn sine opphav, ma de flyttes ut
        // herfra - de star pa listen fordi de kaller nayaktig samme tjeneste.
        $"{nameof(HjemController)}.GiMat",
        $"{nameof(HjemController)}.RegistrerForing",
        $"{nameof(HjemController)}.KryssAv",

        // Handlelisten er felles og lavterskel
        $"{nameof(HandlelisteController)}.Legg",
        $"{nameof(HandlelisteController)}.MarkerKjopt",
        $"{nameof(HandlelisteController)}.Slett",
        $"{nameof(HandlelisteController)}.RyddKjopte"
    ];

    [Fact]
    public void Alle_skrivehandlinger_er_enten_eierbeskyttet_eller_bevisst_apne()
    {
        var controllere = typeof(DyrController).Assembly
            .GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract);

        var udekket = new List<string>();

        foreach (var controller in controllere)
        {
            // Er hele controlleren merket, er alt i den dekket.
            if (controller.GetCustomAttribute<KreverEierAttribute>() is not null)
            {
                continue;
            }

            var poster = controller
                .GetMethods(BindingFlags.Public | BindingFlags.Instance
                            | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpPostAttribute>().Any());

            foreach (var handling in poster)
            {
                var navn = $"{controller.Name}.{handling.Name}";

                if (handling.GetCustomAttribute<KreverEierAttribute>() is not null
                    || ApentForGjest.Contains(navn))
                {
                    continue;
                }

                udekket.Add(navn);
            }
        }

        Assert.True(
            udekket.Count == 0,
            "Disse POST-handlingene mangler [KreverEier], og star heller ikke "
            + "pa listen over det gjester bevisst skal kunne gjore. Ta "
            + "stilling til hvilken av delene som gjelder:\n  "
            + string.Join("\n  ", udekket));
    }

    [Fact]
    public void Gjestelisten_peker_kun_pa_handlinger_som_finnes()
    {
        // Uten denne kan en handling bli omdopt eller fjernet mens navnet
        // blir staende pa gjestelisten - og da beskytter listen ingenting.
        var faktiske = typeof(DyrController).Assembly
            .GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t
                .GetMethods(BindingFlags.Public | BindingFlags.Instance
                            | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpPostAttribute>().Any())
                .Select(m => $"{t.Name}.{m.Name}"))
            .ToHashSet();

        var forsvunnet = ApentForGjest.Where(n => !faktiske.Contains(n)).ToList();

        Assert.True(
            forsvunnet.Count == 0,
            $"Gjestelisten viser til handlinger som ikke finnes: "
            + string.Join(", ", forsvunnet));
    }
}
