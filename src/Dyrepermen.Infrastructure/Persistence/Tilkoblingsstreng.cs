using Npgsql;

namespace Dyrepermen.Infrastructure.Persistence;

/// <summary>
/// Gjor en tilkoblingsstreng om til formen Npgsql forstar.
///
/// Neon og Render oppgir begge databasen som URI:
/// <c>postgresql://bruker:passord@vert/base?sslmode=require</c>. Det er den
/// formen knappen "Copy connection string" gir deg, og den psql tar imot.
///
/// Npgsql tar den IKKE. NpgsqlConnection-konstruktoren venter nokkel/verdi
/// med semikolon, og kaster pa en URI for den har forsokt a kontakte noe som
/// helst. Feilen ser derfor ut som et nettverksproblem uten a vaere det.
///
/// Her oversettes URI-formen, og nokkel/verdi slippes gjennom urort. Da
/// virker appen med det Neon gir deg, uten at noen ma vite dette.
/// </summary>
public static class Tilkoblingsstreng
{
    private const int StandardPort = 5432;

    public static string Normaliser(string verdi)
    {
        var streng = verdi.Trim();

        if (!ErUri(streng))
        {
            return streng;
        }

        var uri = new Uri(streng);
        var brukerinfo = uri.UserInfo.Split(':', 2);

        var bygger = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            // IsDefaultPort er true nar URI-en ikke oppgir port. Uri gir da
            // -1, og det ville blitt sendt videre som port -1.
            Port = uri.IsDefaultPort || uri.Port < 0 ? StandardPort : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),

            // Passord fra Neon inneholder tegn som ma prosentkodes i en URI.
            // Uten avkoding blir "p%40ss" sendt som seks tegn i stedet for
            // fem, og palogging feiler med en melding om feil passord.
            Username = Uri.UnescapeDataString(brukerinfo[0]),
            Password = brukerinfo.Length > 1
                ? Uri.UnescapeDataString(brukerinfo[1])
                : null
        };

        var sporring = System.Web.HttpUtility.ParseQueryString(uri.Query);

        // Neon krever TLS. Require er standard her uansett hva URI-en sier,
        // fordi en tilkobling i klartekst mot en database pa internett ikke
        // er noe vi skal kunne komme til ved et uhell.
        bygger.SslMode = (sporring["sslmode"] ?? "require").ToLowerInvariant() switch
        {
            "disable" => SslMode.Disable,
            "allow" => SslMode.Allow,
            "prefer" => SslMode.Prefer,
            "verify-ca" => SslMode.VerifyCA,
            "verify-full" => SslMode.VerifyFull,
            _ => SslMode.Require
        };

        // Neon Free tar fa samtidige tilkoblinger. Npgsql apner inntil 100 om
        // den far bestemme selv, og databasen avviser dem under last.
        //
        // Settes ubetinget. En URI har ingen mate a uttrykke pooltak pa, sa
        // det finnes ingen verdi her a respektere. ContainsKey duger ikke som
        // vakt heller - NpgsqlConnectionStringBuilder svarer true for alle
        // nokler den kjenner, ogsa de som aldri er satt.
        bygger.MaxPoolSize = 10;

        return bygger.ConnectionString;
    }

    /// <summary>
    /// Begge skrivematene finnes i naturen. Neon bruker <c>postgresql://</c>,
    /// Render og Heroku <c>postgres://</c>.
    /// </summary>
    public static bool ErUri(string verdi)
        => verdi.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        || verdi.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);
}
