using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Kilo/gram begge veier, inkludert avrunding. Plan kapittel 17.3.
/// </summary>
public sealed class VektformatTester
{
    [Theory]
    [InlineData(27400, "27,4 kg")]
    [InlineData(4200, "4,2 kg")]
    [InlineData(5000, "5 kg")]
    [InlineData(500, "0,5 kg")]
    public void Gram_vises_i_kilo_med_komma(int gram, string forventet)
    {
        // Komma, ikke punktum. En engelsk nettleser skal ikke kunne endre det.
        Assert.Equal(forventet, Vektformat.TilKiloTekst(gram));
    }

    [Theory]
    [InlineData(27.4, 27400)]
    [InlineData(4.25, 4250)]
    [InlineData(0.5, 500)]
    [InlineData(5, 5000)]
    public void Kilo_lagres_som_gram(decimal kilo, int forventet)
    {
        Assert.Equal(forventet, Vektformat.TilGram(kilo));
    }

    [Theory]
    [InlineData(1.2345, 1235)]
    [InlineData(1.2344, 1234)]
    [InlineData(1.0005, 1001)]
    public void Avrunding_gar_til_naermeste_gram(decimal kilo, int forventet)
    {
        Assert.Equal(forventet, Vektformat.TilGram(kilo));
    }

    [Fact]
    public void Konvertering_begge_veier_bevarer_verdien()
    {
        const int gram = 27400;
        Assert.Equal(gram, Vektformat.TilGram(gram / 1000m));
    }
}
