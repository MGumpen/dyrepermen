namespace Dyrepermen.Application.Extensions;

/// <summary>
/// Hvilke medisindoser som skal sta i "Forfaller snart".
///
/// Kun medisiner med fast intervall, og kun doser som forfaller i lopet av
/// dagen i norsk tid. En daglig medisin star der hver dag til den er gitt -
/// det er nettopp sporsmalet dashbordet skal svare pa. Ved behov har ingen
/// neste dose, og varsles aldri.
/// </summary>
public static class Dosevarsel
{
    /// <summary>
    /// Tidspunktet neste dose forfaller, eller null nar den ikke skal
    /// varsles i dag. En medisin uten en eneste dose forfaller
    /// <paramref name="naa"/>.
    /// </summary>
    public static DateTimeOffset? NesteDose(
        int intervallTimer,
        DateOnly startDato,
        DateOnly? sluttDato,
        DateTimeOffset? sisteDose,
        DateTimeOffset naa)
    {
        if (intervallTimer <= 0)
        {
            return null;
        }

        var idag = Tidssone.Idag(naa);

        // Sluttdatoen er med: kuren varer til og med den dagen.
        if (startDato > idag || sluttDato is { } slutt && slutt < idag)
        {
            return null;
        }

        var neste = sisteDose?.AddHours(intervallTimer) ?? naa;

        // Norsk dato, ikke UTC. Ellers havner en dose som forfaller kvart
        // over midnatt pa dagens liste.
        return Tidssone.Idag(neste) <= idag ? neste : null;
    }
}
