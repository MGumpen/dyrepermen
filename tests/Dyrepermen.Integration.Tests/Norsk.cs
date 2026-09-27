using System.Globalization;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Forventede verdier formatert slik appen formaterer dem: med nb-NO, ikke
/// med kulturen til maskinen testene kjorer pa.
///
/// $"{dato:d. MMM yyyy}" i en test bruker maskinens kultur. Pa en norsk Mac
/// blir det "25. sep. 2026", som appen skriver - pa CI-maskinen blir det
/// "25. Sep 2026", og testen feiler. Verre: en DoesNotContain med feil format
/// bestar alltid, og tester ingenting.
/// </summary>
internal static class Norsk
{
    public static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("nb-NO");

    public static string Dato(DateOnly dato, string format) => dato.ToString(format, Kultur);
}
