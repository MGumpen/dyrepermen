using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Apningstidene er fritekst per ukedag, og en tom dag betyr stengt.
///
/// Regelen om HVILKE dager som vises, og i hvilken rekkefolge, ligger i
/// Apningstider og ikke i visningene - listen tegnes tre steder, og tre
/// kopier av samme rekkefolge er tre steder den kan komme i utakt.
/// </summary>
public sealed class ApningstiderTester
{
    [Fact]
    public void Bare_dager_med_tid_kommer_med()
    {
        var t = new Apningstider(
            Mandag: "08-16", Tirsdag: null, Onsdag: "10-14",
            Torsdag: null, Fredag: null, Lordag: null, Sondag: null);

        Assert.Equal(["Mandag", "Onsdag"], t.Utfylte.Select(d => d.Dag));
    }

    [Fact]
    public void Rekkefolgen_starter_pa_mandag_ikke_sondag()
    {
        // DayOfWeek i .NET begynner pa sondag. Kom rekkefolgen derfra, ville
        // sondag statt oeverst - og det er ikke slik en uke leses.
        var t = new Apningstider(
            Mandag: "08-16", Tirsdag: null, Onsdag: null,
            Torsdag: null, Fredag: null, Lordag: null, Sondag: "12-15");

        Assert.Equal(["Mandag", "Søndag"], t.Utfylte.Select(d => d.Dag));
    }

    [Fact]
    public void Alle_sju_dagene_kan_fylles_ut()
    {
        var t = new Apningstider("1", "2", "3", "4", "5", "6", "7");

        Assert.Equal(
            ["Mandag", "Tirsdag", "Onsdag", "Torsdag", "Fredag", "Lørdag", "Søndag"],
            t.Utfylte.Select(d => d.Dag));

        Assert.Equal(
            ["Man", "Tir", "Ons", "Tor", "Fre", "Lør", "Søn"],
            t.Utfylte.Select(d => d.Kortdag));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blanke_dager_teller_som_stengt(string tid)
    {
        // Et felt brukeren har vaert innom og forlatt tomt, skal ikke gi en
        // rad med tom tid i oversikten.
        var t = new Apningstider(
            tid, null, null, null, null, null, null);

        Assert.Empty(t.Utfylte);
        Assert.False(t.Finnes);
    }

    [Fact]
    public void Tiden_trimmes_men_beholdes_ellers_som_skrevet()
    {
        // Fritekst med vilje: "10-14, 16-20" og "Dognapent" er begge gyldige,
        // og ingen av dem er to klokkeslett.
        var t = new Apningstider(
            "  10-14, 16-20  ", null, null, null, null, null, "Døgnåpent");

        Assert.Equal("10-14, 16-20", t.Utfylte[0].Tid);
        Assert.Equal("Døgnåpent", t.Utfylte[1].Tid);
    }

    [Fact]
    public void Tom_er_tom()
    {
        Assert.Empty(Apningstider.Tom.Utfylte);
        Assert.False(Apningstider.Tom.Finnes);
    }
}
