namespace Dyrepermen.Web;

/// <summary>
/// Hvor man skal lande etter et husstandsbytte.
///
/// Problemet: returstien er stien du sto pa, og en dyp sti peker pa en
/// RESSURS i husstanden du nettopp forlot. Star du pa /dyr/7 og bytter til en
/// husstand som ikke har dyr 7, moter du en feilside - og det er appen selv
/// som sendte deg dit.
///
/// En 404 er riktig svar for et bokmerke til et fremmed dyr: den skal ikke
/// rope ut at ressursen finnes et annet sted. Men her er det var egen
/// navigasjon, og da skal den lande et sted som finnes.
///
/// Losningen er a beholde SEKSJONEN og slippe ressursen. Sto du pa
/// handlelisten, vil du fortsatt se en handleliste. Sto du pa et bestemt dyr,
/// vil du se dyrene - bare ikke akkurat det dyret, for det er ikke ditt
/// lenger.
/// </summary>
public static class Returadresse
{
    /// <summary>
    /// Seksjoner som finnes i enhver husstand, og som har en forside a lande
    /// pa. Alt annet gar til dashbordet.
    ///
    /// Listen er en tillatelsesliste med vilje: en ukjent sti skal ende pa
    /// oversikten, ikke pa en 404. Legger noen til en ny seksjon uten a fore
    /// den opp her, blir folgen at byttet lander pa dashbordet - kjedelig,
    /// men ikke odelagt. ReturadresseTester sjekker at hver oppforing
    /// faktisk har en forside.
    /// </summary>
    public static readonly IReadOnlySet<string> Seksjoner =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "dyr",
            "handleliste",
            "veterinar",
            "forsikring",
            "informasjon",
            "innstillinger",
            "konto"
        };

    /// <summary>
    /// Gjor en returst til den seksjonen den horer til.
    ///
    /// /dyr/7/vekt gir /dyr. /handleliste gir /handleliste. Alt ukjent, tomt
    /// eller ikke-lokalt gir /.
    /// </summary>
    public static string Seksjon(string? sti)
    {
        if (string.IsNullOrWhiteSpace(sti))
        {
            return "/";
        }

        // Sporrestrengen ma bort FOR oppdelingen. /forplan?dyrId=7 ville
        // ellers batt ID-en videre i forste ledd.
        var bareSti = sti.Split('?', '#')[0];

        var forsteLedd = bareSti
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return forsteLedd is not null && Seksjoner.Contains(forsteLedd)
            ? $"/{forsteLedd.ToLowerInvariant()}"
            : "/";
    }
}
