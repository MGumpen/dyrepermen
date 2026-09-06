using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Regnestykket bak formengden ligger ETT sted. Var det spredt utover
/// tjenestene, kunne dashbordet vist 53 g mens forplansiden viste 54.
/// </summary>
public sealed class ForberegningTester
{
    private static readonly DateOnly Idag = new(2026, 9, 6);

    // 18 uker gammel pa Idag.
    private static readonly DateOnly Fodt = new(2026, 5, 1);

    private static readonly IReadOnlyList<Alderstrinn> Posen =
    [
        new Alderstrinn(3, 160),
        new Alderstrinn(4, 180)
    ];

    private static ForplanResultat Beregn(
        Forplanregel? regel, int? vektGram = 8200, DateOnly? fodt = null)
        => Forberegning.Beregn(regel, new Beregningsgrunnlag(
            vektGram,
            vektGram is null ? null : new DateOnly(2026, 8, 1),
            fodt,
            Idag));

    [Fact]
    public void Uten_plan_finnes_det_ingen_mengde()
        => Assert.False(Beregn(null).HarPlan);

    [Fact]
    public void Fast_mengde_star_stille_uten_vekt()
    {
        var r = Beregn(new Forplanregel(Formetode.Gram, null, 400, 2), vektGram: null);

        Assert.True(r.HarPlan);
        Assert.False(r.ManglerGrunnlag);
        Assert.Equal(400, r.GramPerDag);
    }

    [Fact]
    public void Prosentplan_folger_siste_vekt()
        => Assert.Equal(
            410,
            Beregn(new Forplanregel(Formetode.Prosent, 50, null, 2)).GramPerDag);

    /// <summary>
    /// IKKE 0 gram. Uten vektgrunnlag har tallet ingen dekning, og et tall
    /// uten dekning er verre enn ingen tall. Se plan kapittel 8.1.
    /// </summary>
    [Fact]
    public void Prosentplan_uten_vekt_sier_fra()
    {
        var r = Beregn(
            new Forplanregel(Formetode.Prosent, 50, null, 2), vektGram: null);

        Assert.True(r.HarPlan);
        Assert.True(r.ManglerVekt);
        Assert.Equal(0, r.GramPerDag);
    }

    // ----- Fordeling ---------------------------------------------------

    private static Forplanregel Overgang(int vektdelAndel)
        => new(Formetode.Tabell, 50, null, 2, vektdelAndel, Posen);

    /// <summary>
    /// Andelen skalerer hver komponent for seg. 8,20 kg x 5 % = 410 g rafor,
    /// og 70 % av det er 287 g. Tabellen gir 180 g torrfor ved 18 uker, og
    /// 30 % av det er 54 g.
    ///
    /// Merk at summen, 341 g, IKKE er 410 g delt 70/30. Rafor og torrfor er
    /// ikke sammenlignbare gram for gram, sa en felles dagsmengde delt pa
    /// andelene ville gitt et dyr som gikk ned i vekt gjennom overgangen.
    /// Se ADR 0012.
    /// </summary>
    [Fact]
    public void Fordeling_skalerer_hver_fortype_for_seg()
    {
        var r = Beregn(Overgang(70), fodt: Fodt);

        var b = Assert.IsType<Forfordeling>(r.Fordeling);

        Assert.Equal(410, b.VektdelFullGram);
        Assert.Equal(287, b.VektdelGram);
        Assert.Equal(180, b.AldersdelFullGram);
        Assert.Equal(54, b.AldersdelGram);
        Assert.Equal(30, b.AldersdelAndelProsent);
        Assert.Equal(341, r.GramPerDag);
    }

    [Fact]
    public void Ren_rafor_gir_samme_mengde_som_en_prosentplan()
    {
        var r = Beregn(Overgang(100), fodt: Fodt);

        Assert.Equal(410, r.GramPerDag);
        Assert.Equal(0, r.Fordeling!.AldersdelGram);
    }

    /// <summary>
    /// Star andelen pa 100 % rafor, er torrforet ikke i bruk enda, og da skal
    /// ikke en manglende fodselsdato stoppe planen. Overgangen skal kunne
    /// legges inn for den begynner.
    /// </summary>
    [Fact]
    public void Uten_torrfor_i_blandingen_trengs_ingen_fodselsdato()
    {
        var r = Beregn(Overgang(100));

        Assert.False(r.ManglerGrunnlag);
        Assert.Equal(410, r.GramPerDag);
    }

    /// <summary>Og motsatt: er overgangen fullfort, trengs ingen vekt.</summary>
    [Fact]
    public void Uten_rafor_i_blandingen_trengs_ingen_vekt()
    {
        var r = Beregn(Overgang(0), vektGram: null, fodt: Fodt);

        Assert.False(r.ManglerGrunnlag);
        Assert.Equal(180, r.GramPerDag);
        Assert.Null(r.GrunnlagVektGram);
    }

    [Fact]
    public void Fordeling_uten_fodselsdato_sier_fra_om_alderen()
    {
        var r = Beregn(Overgang(70));

        Assert.True(r.HarPlan);
        Assert.True(r.ManglerFodselsdato);
        Assert.False(r.ManglerVekt);
        Assert.Equal(0, r.GramPerDag);
    }

    [Fact]
    public void Fordeling_uten_vekt_sier_fra_om_vekten()
    {
        var r = Beregn(Overgang(70), vektGram: null, fodt: Fodt);

        Assert.True(r.ManglerVekt);
        Assert.Equal(0, r.GramPerDag);
    }

    /// <summary>
    /// Overgangen i praksis: samme tabell, samme vekt, andelen flyttet
    /// nedover uke for uke. Raforet skal falle og torrforet stige.
    /// </summary>
    [Theory]
    [InlineData(100, 410, 0)]
    [InlineData(75, 308, 45)]
    [InlineData(50, 205, 90)]
    [InlineData(25, 103, 135)]
    [InlineData(0, 0, 180)]
    public void Andelen_flytter_mengden_fra_rafor_til_torrfor(
        int andel, int rafor, int torr)
    {
        var b = Beregn(Overgang(andel), fodt: Fodt).Fordeling!;

        Assert.Equal(rafor, b.VektdelGram);
        Assert.Equal(torr, b.AldersdelGram);
    }

    /// <summary>
    /// En gammel rad kan ha kommet inn for valideringen ble strammet. En
    /// deling pa null ville tatt ned hele dashbordet, ikke bare det ene
    /// kortet.
    /// </summary>
    [Fact]
    public void Null_maltider_faller_tilbake_pa_to()
        => Assert.Equal(
            2,
            Beregn(new Forplanregel(Formetode.Gram, null, 400, 0)).AntallMaltider);

    // ----- Ren tabellplan, uten rafor ---------------------------------

    /// <summary>
    /// Den vanligste bruken av tabellen: en forpose, ingen blanding. Da
    /// trengs verken vekt eller prosentsats - bare alderen.
    /// </summary>
    [Fact]
    public void Ren_tabellplan_trenger_bare_alderen()
    {
        var regel = new Forplanregel(
            Formetode.Tabell, null, null, 2, VektdelAndelProsent: 0, Tabelltrinn: Posen);

        var r = Beregn(regel, vektGram: null, fodt: Fodt);

        Assert.False(r.ManglerGrunnlag);
        Assert.Equal(180, r.GramPerDag);
        Assert.False(r.Fordeling!.Blander);
        Assert.Equal(0, r.Fordeling.VektdelGram);
        Assert.Equal(100, r.Fordeling.AldersdelAndelProsent);
    }

    /// <summary>
    /// Tabellen folger alderen av seg selv. Samme plan, tre ulike dager i
    /// vekstfasen, tre ulike mengder - uten at noen har rort planen.
    /// </summary>
    [Theory]
    [InlineData(91, 160)]    // 13 uker, under forste rad
    [InlineData(98, 164)]    // 14 uker
    [InlineData(105, 169)]   // 15 uker
    [InlineData(126, 180)]   // 18 uker, over siste rad
    public void Mengden_folger_alderen_uten_at_noen_rorer_planen(
        int dagerGammel, int forventet)
    {
        var regel = new Forplanregel(Formetode.Tabell, null, null, 2, 0, Posen);
        var fodt = Idag.AddDays(-dagerGammel);

        Assert.Equal(forventet, Beregn(regel, vektGram: null, fodt: fodt).GramPerDag);
    }

    /// <summary>
    /// Alle dagene i en uke gir samme mengde. En tabell som aldri star
    /// stille er umulig a mate etter.
    /// </summary>
    [Fact]
    public void Mengden_star_stille_gjennom_uka()
    {
        var regel = new Forplanregel(Formetode.Tabell, null, null, 2, 0, Posen);

        var iUka = Enumerable.Range(0, 7)
            .Select(d => Beregn(
                regel, vektGram: null, fodt: Idag.AddDays(-(98 + d))).GramPerDag)
            .Distinct()
            .ToList();

        Assert.Single(iUka);
    }

    /// <summary>
    /// Er dyret utenfor tabellen, holdes mengden - men resultatet sier fra,
    /// sa siden kan be om en rad til.
    /// </summary>
    [Fact]
    public void Utenfor_tabellen_holdes_mengden_og_flagget_settes()
    {
        var regel = new Forplanregel(Formetode.Tabell, null, null, 2, 0, Posen);

        var r = Beregn(regel, vektGram: null, fodt: Idag.AddDays(-400));

        Assert.Equal(180, r.GramPerDag);
        Assert.True(r.Fordeling!.AlderUtenforTabellen);
    }

    /// <summary>
    /// Blander planen ikke inn torrfor i det hele tatt, er tabellen ubrukt -
    /// og da skal den ikke klage pa at alderen er utenfor den.
    /// </summary>
    [Fact]
    public void Ren_raforplan_klager_ikke_pa_en_ubrukt_tabell()
    {
        var regel = new Forplanregel(Formetode.Tabell, 50, null, 2, 100, Posen);

        var r = Beregn(regel, fodt: Idag.AddDays(-400));

        Assert.False(r.Fordeling!.AlderUtenforTabellen);
        Assert.Equal(410, r.GramPerDag);
    }
}
