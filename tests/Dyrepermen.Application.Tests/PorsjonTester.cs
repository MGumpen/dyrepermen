using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Porsjonen regnes ETT sted, sa dashbordet og forplansiden ikke kan vise
/// ulike tall for samme maltid.
/// </summary>
public sealed class PorsjonTester
{
    [Theory]
    [InlineData(200, 2, 100)]
    [InlineData(160, 3, 53)]   // 53,33 rundes ned
    [InlineData(150, 4, 38)]   // 37,5 rundes VEKK fra null, ikke til partall
    [InlineData(90, 1, 90)]
    public void Porsjonen_er_dagsmengden_delt_pa_antall_maltider(
        int gramPerDag, int maltider, int forventet)
        => Assert.Equal(
            forventet,
            ForplanResultat.Ok(gramPerDag, maltider).PorsjonGram);

    [Fact]
    public void Null_maltider_gir_ikke_deling_pa_null()
    {
        // Skjemaet skal hindre det, men en gammel rad kan ha kommet inn for
        // valideringen ble strammet. En krasj pa dashbordet ville tatt ned
        // hele siden - ikke bare det ene kortet.
        Assert.Equal(200, ForplanResultat.Ok(200, 0).PorsjonGram);
    }

    [Fact]
    public void Uten_vektgrunnlag_finnes_det_ingen_mengde()
    {
        // Prosentplan uten registrert vekt: dashbordet skal si fra, ikke
        // vise 0 g. Se plan kapittel 8.1.
        var mangler = ForplanResultat.ManglerVektgrunnlag();

        Assert.True(mangler.HarPlan);
        Assert.True(mangler.ManglerVekt);
    }
}
