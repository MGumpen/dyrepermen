using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// TomTilNull er sma nok til a se triviell ut, men den er det eneste som star
/// mellom et tomt skjemafelt og en unikhetskollisjon pa chipnummer.
/// Se plan kapittel 5.3 og 17.3.
/// </summary>
public sealed class StringExtensionsTester
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n  ")]
    public void Tom_eller_bare_mellomrom_blir_null(string? inn)
    {
        Assert.Null(inn.TomTilNull());
    }

    [Theory]
    [InlineData("578098100000001", "578098100000001")]
    [InlineData("  578098100000001  ", "578098100000001")]
    [InlineData("NO12345/26", "NO12345/26")]
    public void Gyldig_verdi_beholdes_og_trimmes(string inn, string forventet)
    {
        Assert.Equal(forventet, inn.TomTilNull());
    }
}
