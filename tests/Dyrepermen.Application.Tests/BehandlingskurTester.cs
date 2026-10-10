using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// En behandling over flere dager eller flere ganger per dag: nar den gar,
/// hvor mange doser som kan gis, og at dagen skifter ved norsk midnatt.
/// Samtidige trykk dekkes av KurdoseTester mot ekte PostgreSQL. Se ADR 0019.
/// </summary>
public sealed class BehandlingskurTester
{
    // Ormekur en gang daglig i tre dager, fra 10. oktober 2026.
    private static readonly DateOnly ForsteDag = new(2026, 10, 10);

    [Theory]
    [InlineData(1, 1, false)]
    [InlineData(3, 1, true)]
    [InlineData(1, 2, true)]
    [InlineData(3, 2, true)]
    public void Kun_flere_dager_eller_flere_ganger_er_en_kur(
        int antallDager, int gangerPerDag, bool kur)
    {
        Assert.Equal(kur, Behandlingskur.ErKur(antallDager, gangerPerDag));
    }

    [Fact]
    public void Siste_dag_er_til_og_med()
    {
        Assert.Equal(new DateOnly(2026, 10, 12), Behandlingskur.SisteDag(ForsteDag, 3));
        Assert.Equal(ForsteDag, Behandlingskur.SisteDag(ForsteDag, 1));
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(11, true)]
    [InlineData(12, true)]
    [InlineData(13, false)]
    public void Kuren_er_aktiv_fra_forste_til_og_med_siste_dag(int dag, bool aktiv)
    {
        Assert.Equal(aktiv, Behandlingskur.ErAktiv(
            ForsteDag, 3, new DateOnly(2026, 10, dag)));
    }

    [Fact]
    public void Dagnummeret_er_1_pa_forste_dag()
    {
        Assert.Equal(1, Behandlingskur.Dag(ForsteDag, ForsteDag));
        Assert.Equal(3, Behandlingskur.Dag(ForsteDag, new DateOnly(2026, 10, 12)));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    public void Hoyst_ganger_per_dag_doser_per_dag(int doserIdag, bool kanGis)
    {
        Assert.Equal(kanGis, Behandlingskur.KanGiDose(
            ForsteDag, 3, 2, doserIdag, new DateOnly(2026, 10, 11)));
    }

    [Fact]
    public void Ingen_dose_utenfor_perioden()
    {
        Assert.False(Behandlingskur.KanGiDose(ForsteDag, 3, 1, 0, ForsteDag.AddDays(-1)));
        Assert.False(Behandlingskur.KanGiDose(ForsteDag, 3, 1, 0, ForsteDag.AddDays(3)));
    }

    [Fact]
    public void Vanlig_behandling_far_ingen_dose()
    {
        // En dag og en gang regnes som gitt ved registrering.
        Assert.False(Behandlingskur.KanGiDose(ForsteDag, 1, 1, 0, ForsteDag));
    }

    /// <summary>
    /// 22:30 UTC 12. oktober er 00:30 13. oktober i Norge (sommertid, UTC+2).
    /// Kuren er ferdig, selv om UTC-datoen fortsatt er siste dag.
    /// </summary>
    [Fact]
    public void Kuren_slutter_ved_norsk_midnatt_ikke_UTC()
    {
        var naa = new DateTimeOffset(2026, 10, 12, 22, 30, 0, TimeSpan.Zero);

        Assert.False(Behandlingskur.ErAktiv(ForsteDag, 3, Tidssone.Idag(naa)));
        Assert.True(Behandlingskur.ErAktiv(
            ForsteDag, 3, DateOnly.FromDateTime(naa.UtcDateTime)));
    }

    /// <summary>
    /// 23:30 UTC 9. oktober er 01:30 10. oktober i Norge. Forste dag har
    /// startet, selv om UTC-datoen fortsatt er dagen for.
    /// </summary>
    [Fact]
    public void Kuren_starter_ved_norsk_midnatt_ikke_UTC()
    {
        var naa = new DateTimeOffset(2026, 10, 9, 23, 30, 0, TimeSpan.Zero);

        Assert.True(Behandlingskur.ErAktiv(ForsteDag, 3, Tidssone.Idag(naa)));
    }

    /// <summary>
    /// Doser telles fra Tidssone.DagStart. En kveldsdose 23:30 norsk tid
    /// skal ikke telle med pa neste dag, og en dose 00:30 norsk tid skal
    /// telle pa den nye dagen - selv om begge har samme UTC-dato.
    /// </summary>
    [Fact]
    public void Kveldsdosen_teller_pa_dagen_den_ble_gitt()
    {
        // 00:45 11. oktober i Norge.
        var naa = new DateTimeOffset(2026, 10, 10, 22, 45, 0, TimeSpan.Zero);
        var dagStart = Tidssone.DagStart(naa);

        var kveldsdose = new DateTimeOffset(2026, 10, 10, 21, 30, 0, TimeSpan.Zero);
        var nattdose = new DateTimeOffset(2026, 10, 10, 22, 30, 0, TimeSpan.Zero);

        Assert.Equal(new DateTimeOffset(2026, 10, 10, 22, 0, 0, TimeSpan.Zero), dagStart);
        Assert.False(kveldsdose >= dagStart);
        Assert.True(nattdose >= dagStart);
    }

    /// <summary>
    /// Samme grense om vinteren, nar Norge er UTC+1. Forskyvningen hentes pa
    /// midnatt, ikke pa naa.
    /// </summary>
    [Fact]
    public void Dagen_skifter_ved_norsk_midnatt_ogsa_om_vinteren()
    {
        // 00:15 1. desember i Norge.
        var naa = new DateTimeOffset(2026, 11, 30, 23, 15, 0, TimeSpan.Zero);

        Assert.Equal(new DateOnly(2026, 12, 1), Tidssone.Idag(naa));
        Assert.Equal(
            new DateTimeOffset(2026, 11, 30, 23, 0, 0, TimeSpan.Zero),
            Tidssone.DagStart(naa));
    }

    [Theory]
    [InlineData(2, 3, 1, 2, "dag 2 av 3 · 1/2 i dag")]
    [InlineData(1, 1, 0, 2, "0/2 i dag")]
    public void Fremdrift(int dag, int antallDager, int doser, int ganger, string tekst)
    {
        Assert.Equal(tekst, Behandlingskur.Fremdrift(dag, antallDager, doser, ganger));
    }

    [Theory]
    [InlineData(1, 1, null)]
    [InlineData(3, 1, "1 gang daglig i 3 dager")]
    [InlineData(5, 2, "2 ganger daglig i 5 dager")]
    [InlineData(1, 3, "3 ganger samme dag")]
    public void Beskrivelse(int antallDager, int ganger, string? tekst)
    {
        Assert.Equal(tekst, Behandlingskur.Beskrivelse(antallDager, ganger));
    }
}
