using Dyrepermen.Application.Extensions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Grunnlaget bak formengden skal vaere lesbart. Tallet alene sier ikke om
/// planen folger vekten eller star stille, og det er nettopp det brukeren
/// lurer pa nar hun ser "410 g per dag".
/// </summary>
public class ForplanformatTester
{
    // nb-NO grupperer med hardt mellomrom (U+00A0). Det er riktig i HTML -
    // tallet skal ikke brekke over to linjer - men uleselig i en assertion,
    // sa testene sammenligner mot vanlig mellomrom.
    private static string Normaliser(string tekst) => tekst.Replace(' ', ' ');

    [Theory]
    [InlineData(50, "5 %")]
    [InlineData(45, "4,5 %")]
    [InlineData(40, "4 %")]
    [InlineData(5, "0,5 %")]
    public void Prosent_vises_med_komma_og_uten_unodig_desimal(
        int tidels, string forventet)
        => Assert.Equal(forventet, Forplanformat.Prosenttekst(tidels));

    [Theory]
    [InlineData(400, "400 g")]
    [InlineData(1250, "1 250 g")]
    [InlineData(20000, "20 000 g")]
    public void Gram_far_mellomrom_som_tusenskille(int gram, string forventet)
        => Assert.Equal(forventet, Normaliser(Forplanformat.Gramtekst(gram)));

    [Fact]
    public void Prosentplan_sier_at_den_folger_kroppsvekten()
        => Assert.Equal(
            "5 % av kroppsvekten",
            Forplanformat.Regel(Formetode.Prosent, 50, null));

    [Fact]
    public void Fastplan_sier_mengden_i_gram()
        => Assert.Equal(
            "400 g per dag",
            Normaliser(Forplanformat.Regel(Formetode.Gram, null, 400)));

    /// <summary>
    /// Begge leddene skal sta i regnestykket. Ser brukeren bare svaret, ma
    /// hun stole pa appen; ser hun 8,20 kg x 5 %, kan hun regne etter selv.
    /// </summary>
    [Fact]
    public void Utregningen_viser_bade_vekten_og_prosenten()
        => Assert.Equal(
            "8,20 kg × 5 % = 410 g",
            Normaliser(Forplanformat.Utregning(8200, 50, 410)));

    [Fact]
    public void Uten_plan_er_sammendraget_null()
        => Assert.Null(Forplanformat.Sammendrag(null, null, null, null, 8200));

    /// <summary>
    /// IKKE "0 g/dag". Et tall uten vektgrunnlag har ingen dekning, og da er
    /// ingen tall bedre enn et tall. Samme regel som ForplanService.
    /// </summary>
    [Fact]
    public void Prosentplan_uten_vekt_sier_fra_i_stedet_for_a_vise_null()
        => Assert.Equal(
            "Prosentplan – mangler vektregistrering",
            Forplanformat.Sammendrag(Formetode.Prosent, 50, null, 2, null));

    [Fact]
    public void Sammendraget_tar_med_regelen_i_parentes()
        => Assert.Equal(
            "410 g/dag fordelt på 2 måltider (5 % av kroppsvekten)",
            Normaliser(Forplanformat.Sammendrag(Formetode.Prosent, 50, null, 2, 8200)!));

    [Fact]
    public void Sammendraget_for_fast_mengde_nevner_ikke_vekt()
        => Assert.Equal(
            "400 g/dag fordelt på 3 måltider",
            Normaliser(Forplanformat.Sammendrag(Formetode.Gram, null, 400, 3, 8200)!));

    /// <summary>
    /// Avrunding bort fra null, samme som ForplanService. 8205 g x 5,0 %
    /// er 410,25 - og 8210 x 5,0 % er 410,5, som skal bli 411 og ikke 410.
    /// </summary>
    [Fact]
    public void Halve_gram_rundes_opp_slik_ForplanService_gjor()
        => Assert.Equal(
            "411 g/dag fordelt på 2 måltider (5 % av kroppsvekten)",
            Normaliser(Forplanformat.Sammendrag(Formetode.Prosent, 50, null, 2, 8210)!));
}
