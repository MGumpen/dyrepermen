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

    /// <summary>
    /// Ved blanding er porsjonen summen av de to delene, ikke dagsmengden
    /// delt pa antall maltider. De to kan skille ett gram - og et kort som
    /// viser "5 g" over "3 g rafor + 3 g torrfor" ser ut som en regnefeil,
    /// fordi det er en.
    /// </summary>
    [Fact]
    public void Blandingsporsjonen_er_summen_av_de_to_delene()
    {
        // 5 g og 5 g pa to maltider: hver del rundes til 3, sa porsjonen er
        // 6 - ikke 5, som 10/2 ville gitt.
        var r = ForplanResultat.OkFordelt(
            2, new Forfordeling(5, 50, 5, 10, 5, 18));

        Assert.Equal(3, r.VektdelPorsjonGram);
        Assert.Equal(3, r.AldersdelPorsjonGram);
        Assert.Equal(6, r.PorsjonGram);
    }

    [Fact]
    public void Uten_blanding_finnes_det_ingen_deling()
    {
        var r = ForplanResultat.Ok(200, 2);

        Assert.Equal(0, r.VektdelPorsjonGram);
        Assert.Equal(0, r.AldersdelPorsjonGram);
        Assert.Equal(100, r.PorsjonGram);
    }

    [Fact]
    public void Blandingen_deles_ogsa_nar_maltidene_star_pa_null()
    {
        // Deling pa null ville tatt ned hele dashbordet.
        var r = ForplanResultat.OkFordelt(
            0, new Forfordeling(410, 70, 287, 180, 54, 18));

        Assert.Equal(287, r.VektdelPorsjonGram);
        Assert.Equal(54, r.AldersdelPorsjonGram);
        Assert.Equal(341, r.PorsjonGram);
    }
}
