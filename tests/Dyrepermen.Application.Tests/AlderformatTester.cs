using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Application.Tests;

public sealed class AlderformatTester
{
    private static readonly DateOnly Idag = new(2026, 8, 10);

    [Theory]
    [InlineData(2022, 5, 17, "4 år")]
    [InlineData(2025, 8, 10, "1 år")]
    [InlineData(2025, 8, 11, "11 mnd")]
    [InlineData(2026, 5, 10, "3 mnd")]
    [InlineData(2026, 8, 10, "0 mnd")]
    public void Alder_regnes_riktig(int ar, int mnd, int dag, string forventet)
    {
        Assert.Equal(forventet, Alderformat.Tekst(new DateOnly(ar, mnd, dag), Idag));
    }

    [Fact]
    public void Bursdag_som_ikke_har_vaert_enna_teller_ikke_med()
    {
        // Fodt 11. august 2022, i dag 10. august 2026: fyller 4 i morgen.
        Assert.Equal("3 år", Alderformat.Tekst(new DateOnly(2022, 8, 11), Idag));
    }
}
