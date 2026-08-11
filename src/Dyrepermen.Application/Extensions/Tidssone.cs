using System.Globalization;

namespace Dyrepermen.Application.Extensions;

/// <summary>
/// All lagring skjer i UTC. Konvertering til visning gjores ETT sted - her.
///
/// Uten dette forskyver alle registreringer seg med en time ved
/// sommertidsomstillingen, og feilen viser seg to ganger i aret pa data som
/// var riktig da den ble lagret. Se plan kapittel 7.3 og 8.2.
///
/// IANA-navnet "Europe/Oslo", ikke Windows-navnet. .NET pa Linux stotter
/// begge fra .NET 8, men IANA er det som virker i containeren uten ekstra
/// pakker.
/// </summary>
public static class Tidssone
{
    private static readonly TimeZoneInfo Oslo =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

    // Eksplisitt kultur, ikke tradens. Applikasjonen kjorer nb-NO gjennom
    // RequestLocalization, men en bakgrunnsjobb eller en test gjor ikke det -
    // og da ville manedsnavnet blitt engelsk. Samme monster som Vektformat.
    private static readonly CultureInfo Norsk = new("nb-NO");

    public static DateTimeOffset TilLokal(DateTimeOffset tid)
        => TimeZoneInfo.ConvertTime(tid, Oslo);

    /// <summary>
    /// Forskyvningen som gjelder i Norge pa et gitt lokalt tidspunkt.
    /// +01:00 om vinteren, +02:00 om sommeren. Trengs nar et skjema sender
    /// lokal tid uten sone, og verdien skal tolkes riktig.
    /// </summary>
    public static TimeSpan Forskyvning(DateTime lokalTid)
        => Oslo.GetUtcOffset(DateTime.SpecifyKind(lokalTid, DateTimeKind.Unspecified));

    /// <summary>"07:12" i norsk lokaltid.</summary>
    public static string Klokke(DateTimeOffset tid)
        => TilLokal(tid).ToString("HH:mm", Norsk);

    /// <summary>
    /// "i dag 07:12", "i gar 19:30" eller "3. aug 07:12". Relativt der det
    /// hjelper, absolutt der det trengs.
    /// </summary>
    public static string NaerTid(DateTimeOffset tid, DateTimeOffset naa)
    {
        var lokal = TilLokal(tid);
        var idag = TilLokal(naa).Date;
        var dag = lokal.Date;

        if (dag == idag)
        {
            return string.Create(Norsk, $"i dag {lokal:HH:mm}");
        }

        if (dag == idag.AddDays(-1))
        {
            return string.Create(Norsk, $"i går {lokal:HH:mm}");
        }

        return lokal.ToString("d. MMM HH:mm", Norsk);
    }
}
