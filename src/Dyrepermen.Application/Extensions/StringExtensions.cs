namespace Dyrepermen.Application.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Normaliserer tom streng og bare mellomrom til null.
    ///
    /// Ma brukes pa chipnummer og regnummer for lagring. Et skjemafelt som ikke
    /// fylles ut sender "", ikke null - og to dyr uten chipnummer vil da
    /// kollidere i den partielle unike indeksen, siden tom streng er en verdi
    /// mens NULL ikke deltar i unikhetssjekken. Se plan kapittel 5.3.
    /// </summary>
    public static string? TomTilNull(this string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
