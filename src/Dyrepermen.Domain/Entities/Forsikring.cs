using Dyrepermen.Domain.Abstractions;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Forsikringspolise for ett dyr. <see cref="FornyesDato"/> driver
/// paminnelsen pa dashbordet.
///
/// Norsk dyreforsikring har som regel TO egenandeler: en fast sum, og en
/// variabel andel av det som overstiger den. Uten begge kan man ikke regne
/// ut hva et veterinaerbesok faktisk koster. Se plan kapittel 4.2.
/// </summary>
public sealed class Forsikring : IHusstandsbundet
{
    public int Id { get; set; }

    public int DyrId { get; set; }

    public Dyr Dyr { get; set; } = null!;

    public string Selskap { get; set; } = null!;

    public string? PoliseNr { get; set; }

    /// <summary>Hele kroner per ar.</summary>
    public int ArspremieKr { get; set; }

    /// <summary>Dekningssum per ar, hele kroner.</summary>
    public int ForsikringsbelopKr { get; set; }

    /// <summary>Fast egenandel i hele kroner.</summary>
    public int EgenandelFastKr { get; set; }

    /// <summary>
    /// Tidels prosent: 200 betyr 20,0 %. Samme monster som
    /// forplan.prosent_tidels - hele modellen holder seg til INT, og all
    /// aritmetikk blir eksakt.
    /// </summary>
    public int EgenandelVariabelTidels { get; set; }

    public DateOnly? FornyesDato { get; set; }

    /// <summary>
    /// Hva du selv betaler av en regning pa <paramref name="regningKr"/>.
    /// Fast egenandel forst, deretter den variable andelen av det som er
    /// igjen. Ren regning uten avhengigheter, sa den kan enhetstestes.
    /// </summary>
    public int Egenandel(int regningKr)
    {
        if (regningKr <= EgenandelFastKr)
        {
            return regningKr;
        }

        var overskytende = regningKr - EgenandelFastKr;
        var variabel = (int)Math.Round(
            overskytende * EgenandelVariabelTidels / 1000.0,
            MidpointRounding.AwayFromZero);

        return EgenandelFastKr + variabel;
    }
}
