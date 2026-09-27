namespace Dyrepermen.Application.Extensions;

/// <summary>
/// Neste gang for en behandling som gjentas.
///
/// Nar en paminnelse krysses av som gitt, far den nye behandlingen samme
/// intervall som den forrige. Intervallet gjenkjennes som hele maneder nar
/// det er det: ormekur "hver tredje maned" og vaksine "hvert ar" er slik folk
/// og veterinaerer tenker, og 92 dager ville latt datoen krype en dag eller to
/// for hver gang.
/// </summary>
public static class Behandlingsintervall
{
    /// <summary>
    /// Om en behandling gitt <paramref name="forrigeDato"/> kan krysses av
    /// som gitt pa nytt i dag.
    ///
    /// Ikke hvis den ble gitt i dag eller har en dato fram i tid. Da ville den
    /// nye raden fatt samme dato som den gamle - og med et intervall pa en
    /// dag ogsa samme neste gang. Raden ville sett uendret ut, med en ny knapp
    /// som ga enda en kopi.
    /// </summary>
    public static bool KanGisIgjen(DateOnly forrigeDato, DateOnly idag)
        => forrigeDato < idag;

    /// <summary>Ti ar. Lengre enn det er ikke et intervall, men en tastefeil.</summary>
    private const int MaksManeder = 120;

    /// <summary>
    /// Null nar forrige behandling ikke hadde noen neste gang, eller nar den
    /// la pa eller for selve behandlingsdatoen.
    /// </summary>
    public static DateOnly? NesteEtter(
        DateOnly forrigeDato, DateOnly? forrigeNeste, DateOnly nyDato)
    {
        if (forrigeNeste is not { } neste || neste <= forrigeDato)
        {
            return null;
        }

        for (var maneder = 1; maneder <= MaksManeder; maneder++)
        {
            var kandidat = forrigeDato.AddMonths(maneder);

            if (kandidat == neste)
            {
                return nyDato.AddMonths(maneder);
            }

            if (kandidat > neste)
            {
                break;
            }
        }

        return nyDato.AddDays(neste.DayNumber - forrigeDato.DayNumber);
    }
}
