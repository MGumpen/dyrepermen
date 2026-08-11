using Dyrepermen.Domain.Entities;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Norsk dyreforsikring har to egenandeler: en fast sum, og en variabel
/// andel av det som overstiger den. Regnestykket er hele grunnen til at
/// begge lagres separat. Se plan kapittel 4.2.
/// </summary>
public sealed class EgenandelTester
{
    private static Forsikring Polise(int fast, int variabelTidels)
        => new()
        {
            Selskap = "Agria",
            EgenandelFastKr = fast,
            EgenandelVariabelTidels = variabelTidels
        };

    [Fact]
    public void Regning_under_fast_egenandel_betales_i_sin_helhet()
    {
        // 1500 fast, regning pa 900: du betaler alt selv. Forsikringen
        // slar ikke inn i det hele tatt.
        Assert.Equal(900, Polise(1500, 200).Egenandel(900));
    }

    [Fact]
    public void Regning_lik_fast_egenandel_gir_ingen_variabel_andel()
    {
        Assert.Equal(1500, Polise(1500, 200).Egenandel(1500));
    }

    [Fact]
    public void Variabel_andel_regnes_kun_av_det_overskytende()
    {
        // 1500 fast + 20 % av (10000 - 1500) = 1500 + 1700 = 3200.
        // Regnes den av hele regningen, blir svaret 3500 - og det er feil
        // vei for brukeren.
        Assert.Equal(3200, Polise(1500, 200).Egenandel(10000));
    }

    [Fact]
    public void Null_variabel_gir_kun_den_faste()
    {
        Assert.Equal(1500, Polise(1500, 0).Egenandel(10000));
    }

    [Theory]
    [InlineData(2000, 225, 10000, 3800)]
    [InlineData(0, 200, 5000, 1000)]
    [InlineData(1000, 1000, 5000, 5000)]
    public void Kjente_kombinasjoner(
        int fast, int tidels, int regning, int forventet)
    {
        Assert.Equal(forventet, Polise(fast, tidels).Egenandel(regning));
    }

    [Fact]
    public void Avrunding_gar_til_naermeste_krone()
    {
        // 22,5 % av 1001 = 225,225 -> 225.
        Assert.Equal(225, Polise(0, 225).Egenandel(1001));
    }
}
