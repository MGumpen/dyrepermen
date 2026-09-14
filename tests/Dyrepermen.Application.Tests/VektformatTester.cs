using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Kilo/gram begge veier, inkludert avrunding. Plan kapittel 17.3.
/// </summary>
public sealed class VektformatTester
{
    [Theory]
    [InlineData(27400, "27,40 kg")]
    [InlineData(4200, "4,20 kg")]
    [InlineData(5000, "5,00 kg")]
    [InlineData(500, "0,50 kg")]
    public void Gram_vises_i_kilo_med_komma_og_minst_to_desimaler(
        int gram, string forventet)
    {
        // Komma, ikke punktum. En engelsk nettleser skal ikke kunne endre det.
        Assert.Equal(forventet, Vektformat.TilKiloTekst(gram));
    }

    [Theory]
    [InlineData(3150, "3,15 kg")]
    [InlineData(3155, "3,155 kg")]
    [InlineData(1, "0,001 kg")]
    public void Visningen_avrunder_aldri_bort_presisjon(int gram, string forventet)
    {
        // 3150 gram vist som "3,2 kg" er et tall brukeren aldri skrev inn.
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
