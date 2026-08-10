using System.Globalization;

namespace Dyrepermen.Application.Extensions;

/// <summary>
/// Vekt lagres i gram som heltall, slik at all aritmetikk blir eksakt.
/// Formatering hoerer hjemme i visningslaget - her. Se plan kapittel 5.2.
/// </summary>
public static class Vektformat
{
    private static readonly CultureInfo Norsk = new("nb-NO");

    /// <summary>27400 gir "27,4 kg". Komma, ikke punktum.</summary>
    public static string TilKiloTekst(int gram)
        => string.Create(Norsk, $"{gram / 1000m:0.#} kg");

    /// <summary>27,4 gir 27400. Avrunder til naermeste gram.</summary>
    public static int TilGram(decimal kilo)
        => (int)Math.Round(kilo * 1000m, MidpointRounding.AwayFromZero);
}
