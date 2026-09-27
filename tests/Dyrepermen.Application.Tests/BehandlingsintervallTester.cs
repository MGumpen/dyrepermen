using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Nar en behandling krysses av som gitt, settes neste gang med samme
/// intervall som forrige gang. Intervallet skal gjenkjennes slik brukeren
/// tenkte det - "om tre maneder", ikke "om 92 dager".
/// </summary>
public sealed class BehandlingsintervallTester
{
    [Fact]
    public void Hele_maneder_gjentas_som_maneder()
    {
        // 1. januar til 1. april er 90 dager, men tre maneder. Gitt 1. mai
        // skal neste bli 1. august - ikke 30. juli.
        var neste = Behandlingsintervall.NesteEtter(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1), new DateOnly(2026, 5, 1));

        Assert.Equal(new DateOnly(2026, 8, 1), neste);
    }

    [Fact]
    public void Ett_ar_gjentas_som_ett_ar()
    {
        var neste = Behandlingsintervall.NesteEtter(
            new DateOnly(2025, 3, 15), new DateOnly(2026, 3, 15), new DateOnly(2026, 3, 20));

        Assert.Equal(new DateOnly(2027, 3, 20), neste);
    }

    [Fact]
    public void Manedsslutt_klemmes_til_siste_dag_i_maneden()
    {
        // 31. januar pluss en maned finnes ikke. DateOnly.AddMonths gir 28.
        var neste = Behandlingsintervall.NesteEtter(
            new DateOnly(2026, 3, 1), new DateOnly(2026, 4, 1), new DateOnly(2027, 1, 31));

        Assert.Equal(new DateOnly(2027, 2, 28), neste);
    }

    [Fact]
    public void Andre_intervaller_gjentas_som_dager()
    {
        // 6 uker er ikke et helt antall maneder.
        var neste = Behandlingsintervall.NesteEtter(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 12), new DateOnly(2026, 3, 1));

        Assert.Equal(new DateOnly(2026, 4, 12), neste);
    }

    [Fact]
    public void Uten_neste_dato_finnes_ingen_ny_neste_dato()
    {
        Assert.Null(Behandlingsintervall.NesteEtter(
            new DateOnly(2026, 1, 1), null, new DateOnly(2026, 3, 1)));
    }

    [Fact]
    public void Neste_dato_for_eller_lik_datoen_gir_ingen_ny_neste_dato()
    {
        // Et intervall pa null eller mindre dager ville gitt en paminnelse
        // som forfaller samme dag som den ble fulgt opp - eller i fortiden.
        Assert.Null(Behandlingsintervall.NesteEtter(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 1)));
        Assert.Null(Behandlingsintervall.NesteEtter(
            new DateOnly(2026, 1, 1), new DateOnly(2025, 12, 1), new DateOnly(2026, 3, 1)));
    }
}
