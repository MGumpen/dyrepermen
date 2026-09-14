using System.Globalization;

namespace Dyrepermen.Application.Extensions;

/// <summary>
/// Vekt lagres i gram som heltall, slik at all aritmetikk blir eksakt.
/// Formatering hoerer hjemme i visningslaget - her. Se plan kapittel 5.2.
/// </summary>
public static class Vektformat
{
    private static readonly CultureInfo Norsk = new("nb-NO");

    /// <summary>
    /// 27400 gir "27,40 kg", 3150 gir "3,15 kg", 3155 gir "3,155 kg".
    ///
    /// Minst to desimaler, men aldri avrunding: monsteret "0.00#" gir to
    /// faste og en valgfri tredje. Ett gram er en tusendels kilo, sa tre
    /// desimaler dekker hele presisjonen databasen kan lagre.
    ///
    /// Et format med faerre desimaler ville vist 3150 gram som "3,2 kg" -
    /// et tall som ser ut som noe brukeren aldri skrev inn.
    /// </summary>
    public static string TilKiloTekst(int gram)
        => string.Create(Norsk, $"{gram / 1000m:0.00#} kg");

    /// <summary>27,4 gir 27400. Avrunder til naermeste gram.</summary>
    public static int TilGram(decimal kilo)
        => (int)Math.Round(kilo * 1000m, MidpointRounding.AwayFromZero);
}
