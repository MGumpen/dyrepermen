using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Akseptansekriterium i fase 3: maltidsfordelingen skal summere eksakt til
/// dagsmengden. Se plan kapittel 16.
/// </summary>
public sealed class MaltidsfordelingTester
{
    [Theory]
    [InlineData(400, 2)]
    [InlineData(401, 3)]
    [InlineData(1, 6)]
    [InlineData(0, 3)]
    [InlineData(999, 4)]
    [InlineData(27400, 5)]
    public void Summen_stemmer_alltid_med_dagsmengden(int gram, int antall)
    {
        // Dette er hele poenget. Heltallsdivisjon uten resthandtering ville
        // "mistet" gram hver eneste dag.
        Assert.Equal(gram, Maltidsfordeling.Fordel(gram, antall).Sum());
    }

    [Theory]
    [InlineData(400, 2, new[] { 200, 200 })]
    [InlineData(401, 3, new[] { 134, 134, 133 })]
    [InlineData(7, 3, new[] { 3, 2, 2 })]
    public void Resten_legges_pa_de_forste_maltidene(
        int gram, int antall, int[] forventet)
    {
        Assert.Equal(forventet, Maltidsfordeling.Fordel(gram, antall));
    }

    [Fact]
    public void Antall_maltider_bestemmer_lengden()
    {
        Assert.Equal(4, Maltidsfordeling.Fordel(100, 4).Length);
    }

    [Fact]
    public void Null_maltider_gir_tom_fordeling_i_stedet_for_deling_pa_null()
    {
        Assert.Empty(Maltidsfordeling.Fordel(100, 0));
    }
}
