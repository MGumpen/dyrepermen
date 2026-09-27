using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Hvilke medisindoser som dukker opp i "Forfaller snart" pa dashbordet:
/// de med fast intervall som forfaller i lopet av dagen, i norsk tid.
/// </summary>
public sealed class DosevarselTester
{
    // 14. mai 2026 klokka 10:00 i Norge (sommertid, UTC+2).
    private static readonly DateTimeOffset Naa =
        new(2026, 5, 14, 8, 0, 0, TimeSpan.Zero);

    private static readonly DateOnly Idag = new(2026, 5, 14);

    [Fact]
    public void Dose_som_forfaller_senere_i_dag_varsles()
    {
        var siste = Naa.AddHours(-6);

        Assert.Equal(siste.AddHours(12), Dosevarsel.NesteDose(
            12, Idag.AddDays(-3), null, siste, Naa));
    }

    [Fact]
    public void Dose_som_forfaller_i_morgen_varsles_ikke()
    {
        // Gitt 09:00 med 24 timers intervall - neste er 09:00 i morgen.
        var siste = Naa.AddHours(-1);

        Assert.Null(Dosevarsel.NesteDose(24, Idag.AddDays(-3), null, siste, Naa));
    }

    [Fact]
    public void Dose_som_er_over_tiden_varsles()
    {
        // Glemt i to dogn - skal fortsatt sta der, ikke forsvinne.
        var siste = Naa.AddDays(-3);

        Assert.Equal(siste.AddHours(24), Dosevarsel.NesteDose(
            24, Idag.AddDays(-5), null, siste, Naa));
    }

    [Fact]
    public void Medisin_uten_doser_forfaller_na()
    {
        Assert.Equal(Naa, Dosevarsel.NesteDose(12, Idag, null, null, Naa));
    }

    [Fact]
    public void Ved_behov_varsles_aldri()
    {
        Assert.Null(Dosevarsel.NesteDose(0, Idag.AddDays(-3), null, null, Naa));
    }

    [Fact]
    public void Medisin_som_ikke_har_startet_varsles_ikke()
    {
        Assert.Null(Dosevarsel.NesteDose(12, Idag.AddDays(1), null, null, Naa));
    }

    [Fact]
    public void Avsluttet_medisin_varsles_ikke()
    {
        Assert.Null(Dosevarsel.NesteDose(
            12, Idag.AddDays(-10), Idag.AddDays(-1), Naa.AddDays(-2), Naa));
    }

    [Fact]
    public void Siste_dag_av_kuren_varsles()
    {
        // Sluttdatoen er inkludert - kuren varer til og med den dagen.
        var siste = Naa.AddHours(-13);

        Assert.NotNull(Dosevarsel.NesteDose(12, Idag.AddDays(-10), Idag, siste, Naa));
    }

    [Fact]
    public void Dagsgrensen_er_norsk_midnatt_ikke_UTC()
    {
        // Klokka er 22:00 i Norge. En dose som forfaller 00:30 norsk tid er
        // 22:30 UTC - fortsatt 14. mai i UTC, men 15. mai her. Den horer til
        // i morgen. En som forfaller 23:30 norsk tid horer til i dag.
        var kveld = new DateTimeOffset(2026, 5, 14, 20, 0, 0, TimeSpan.Zero);

        Assert.Null(Dosevarsel.NesteDose(
            12, Idag.AddDays(-3), null, kveld.AddHours(-9.5), kveld));
        Assert.NotNull(Dosevarsel.NesteDose(
            12, Idag.AddDays(-3), null, kveld.AddHours(-10.5), kveld));
    }
}
