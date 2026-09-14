using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Skaleringen er ren logikk, sa den kan testes uten a rendre noe.
/// </summary>
public sealed class VektgrafTester
{
    private static (DateOnly, int) M(int ar, int mnd, int dag, int gram)
        => (new DateOnly(ar, mnd, dag), gram);

    [Fact]
    public void Faerre_enn_to_malinger_gir_ingen_graf()
    {
        // En enkelt maling er et tall, ikke en graf. Da sier
        // historikktabellen det som er a si.
        Assert.Null(Vektgrafberegning.Beregn([]));
        Assert.Null(Vektgrafberegning.Beregn([M(2026, 1, 1, 5000)]));
    }

    [Fact]
    public void X_folger_faktisk_tid_ikke_rekkefolge()
    {
        // Tre malinger tett i januar og en i desember skal IKKE ligge jevnt
        // fordelt - da ville grafen vist en jevn vekst som ikke fant sted.
        var graf = Vektgrafberegning.Beregn([
            M(2026, 1, 1, 5000),
            M(2026, 1, 8, 5200),
            M(2026, 12, 31, 9000)
        ])!;

        var spenn = graf.Punkter[^1].X - graf.Punkter[0].X;
        var forsteBit = graf.Punkter[1].X - graf.Punkter[0].X;

        // Sju dager av et helt ar er under 5 % av bredden.
        Assert.True(forsteBit / spenn < 0.05,
            $"Forste uke tok {forsteBit / spenn:P1} av bredden");
    }

    [Fact]
    public void Hoyere_vekt_gir_lavere_Y()
    {
        // Y vokser nedover i SVG. Snur dette seg, star grafen pa hodet.
        var graf = Vektgrafberegning.Beregn([
            M(2026, 1, 1, 5000),
            M(2026, 6, 1, 9000)
        ])!;

        Assert.True(graf.Punkter[1].Y < graf.Punkter[0].Y);
    }

    [Fact]
    public void Punktene_holder_seg_innenfor_flaten()
    {
        var graf = Vektgrafberegning.Beregn([
            M(2026, 1, 1, 1),
            M(2026, 3, 1, 60000),
            M(2026, 6, 1, 27400)
        ])!;

        Assert.All(graf.Punkter, p =>
        {
            Assert.InRange(p.X, 0, graf.Bredde);
            Assert.InRange(p.Y, 0, graf.Hoyde);
        });
    }

    [Fact]
    public void Aksemerkene_er_runde_tall()
    {
        // Uten avrunding blir etikettene 3,47 og 4,12 - tall ingen leser av
        // en akse.
        var graf = Vektgrafberegning.Beregn([
            M(2026, 1, 1, 3150),
            M(2026, 6, 1, 4870)
        ])!;

        Assert.NotEmpty(graf.Merker);

        var steg = graf.Merker[1].Gram - graf.Merker[0].Gram;
        Assert.All(graf.Merker, m => Assert.Equal(0, m.Gram % steg));
    }

    [Fact]
    public void Like_malinger_gir_fortsatt_en_gyldig_graf()
    {
        // Uten kunstig spenn ville skaleringen delt pa null.
        var graf = Vektgrafberegning.Beregn([
            M(2026, 1, 1, 5000),
            M(2026, 2, 1, 5000),
            M(2026, 3, 1, 5000)
        ])!;

        Assert.All(graf.Punkter, p => Assert.InRange(p.Y, 0, graf.Hoyde));
        Assert.NotEmpty(graf.Merker);
    }

    [Fact]
    public void Malinger_sorteres_selv_om_de_kommer_i_omvendt_rekkefolge()
    {
        // Tjenesten leverer nyeste forst. Grafen ma tegne eldste forst.
        var graf = Vektgrafberegning.Beregn([
            M(2026, 6, 1, 9000),
            M(2026, 1, 1, 5000)
        ])!;

        Assert.Equal(new DateOnly(2026, 1, 1), graf.Punkter[0].Dato);
        Assert.True(graf.Punkter[0].X < graf.Punkter[1].X);
    }

    [Fact]
    public void Linja_og_flaten_er_gyldige_stier()
    {
        var graf = Vektgrafberegning.Beregn([
            M(2026, 1, 1, 5000),
            M(2026, 6, 1, 9000)
        ])!;

        Assert.StartsWith("M", graf.Linje);
        Assert.Contains("L", graf.Linje);

        // Flaten lukkes mot bunnlinja.
        Assert.EndsWith("Z", graf.Flate);

        // Punktum som desimalskilletegn - komma ville delt SVG-koordinatene.
        Assert.DoesNotContain(",,", graf.Linje);
        Assert.Matches(@"^M[\d.]+,[\d.]+", graf.Linje);
    }
}
