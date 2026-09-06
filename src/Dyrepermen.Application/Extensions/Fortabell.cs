using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Extensions;

/// <summary>
/// Slar opp dagsmengden i tabellen brukeren har skrevet av fra forposen -
/// alder i maneder mot gram per dag - og trapper jevnt mellom radene.
///
/// Appen anbefaler ingen mengde. Tallene er avskrift av posen, og tabellen
/// er tom til noen fyller den ut. Se plan kapittel 8.1.
///
/// **Alderen rundes ned til hele uker for oppslaget.** Regnet per dag ville
/// mengden endret seg med noen gram hver morgen, og en tabell som aldri star
/// stille er umulig a mate etter. Med uker star tallet i sju dager og tar ett
/// hopp.
///
/// Utenfor tabellen ekstrapoleres det ikke. Under forste rad gjelder forste
/// rad, over siste gjelder siste - og oppslaget sier fra at det skjedde, sa
/// grensesnittet kan be om en rad til i stedet for a la et flatt tall se ut
/// som en beregning.
/// </summary>
public static class Fortabell
{
    // Gjennomsnittsmaneden, ikke 30 dager. Over et forste leveaar utgjor
    // forskjellen drovt en uke, og det er en uke midt i vekstfasen.
    private const double DagerPerManed = 365.25 / 12;

    public static Taboppslag Slaopp(IReadOnlyList<Alderstrinn> trinn, int alderUker)
    {
        if (trinn.Count == 0)
        {
            return new Taboppslag(0, false);
        }

        var sortert = trinn.OrderBy(t => t.AlderMnd).ToList();
        var alderMnd = alderUker * 7 / DagerPerManed;

        if (alderMnd < sortert[0].AlderMnd)
        {
            return new Taboppslag(sortert[0].GramPerDag, true);
        }

        var siste = sortert[^1];

        if (alderMnd > siste.AlderMnd)
        {
            return new Taboppslag(siste.GramPerDag, true);
        }

        for (var i = 1; i < sortert.Count; i++)
        {
            var over = sortert[i];

            if (alderMnd > over.AlderMnd)
            {
                continue;
            }

            var under = sortert[i - 1];
            var spenn = over.AlderMnd - under.AlderMnd;

            // ux_forplantrinn_alder hindrer to rader pa samme maned, men en
            // deling pa null her ville tatt ned hele siden.
            if (spenn <= 0)
            {
                return new Taboppslag(over.GramPerDag, false);
            }

            var andel = (alderMnd - under.AlderMnd) / spenn;

            return new Taboppslag(
                (int)Math.Round(
                    under.GramPerDag + andel * (over.GramPerDag - under.GramPerDag),
                    MidpointRounding.AwayFromZero),
                false);
        }

        return new Taboppslag(siste.GramPerDag, false);
    }

    /// <summary>Bare mengden, der oppslagets utenfor-flagg ikke trengs.</summary>
    public static int GramVedUker(IReadOnlyList<Alderstrinn> trinn, int alderUker)
        => Slaopp(trinn, alderUker).Gram;
}
