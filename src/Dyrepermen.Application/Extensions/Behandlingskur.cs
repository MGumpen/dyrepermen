namespace Dyrepermen.Application.Extensions;

/// <summary>
/// En behandling som gar over flere dager eller gis flere ganger per dag -
/// ormekur en gang daglig i tre dager. Se ADR 0019.
///
/// Forste dag er behandlingens dato, og perioden er til og med siste dag.
/// "I dag" er alltid norsk dato fra <see cref="Tidssone.Idag"/>, og doser
/// telles fra <see cref="Tidssone.DagStart"/> - ellers flytter en kveldsdose
/// seg til neste dag klokka ett eller to om natten.
///
/// Samme regel i sporringen: Behandlingsfilter.AktivKur. Endres den ene, ma
/// den andre folge med.
/// </summary>
public static class Behandlingskur
{
    /// <summary>
    /// En dag og en gang er en vanlig behandling. Den regnes som gitt nar
    /// den registreres, og far ingen doser og ingen knapp.
    /// </summary>
    public static bool ErKur(int antallDager, int gangerPerDag)
        => antallDager > 1 || gangerPerDag > 1;

    public static DateOnly SisteDag(DateOnly forsteDag, int antallDager)
        => forsteDag.AddDays(antallDager - 1);

    public static bool ErAktiv(DateOnly forsteDag, int antallDager, DateOnly idag)
        => forsteDag <= idag && idag <= SisteDag(forsteDag, antallDager);

    /// <summary>1 pa forste dag.</summary>
    public static int Dag(DateOnly forsteDag, DateOnly idag)
        => idag.DayNumber - forsteDag.DayNumber + 1;

    /// <summary>
    /// Om en dose til kan registreres na: en kur, aktiv i dag, og dagens
    /// doser er ikke fullfort.
    /// </summary>
    public static bool KanGiDose(
        DateOnly forsteDag, int antallDager, int gangerPerDag,
        int doserIdag, DateOnly idag)
        => ErKur(antallDager, gangerPerDag)
            && ErAktiv(forsteDag, antallDager, idag)
            && doserIdag < gangerPerDag;

    /// <summary>
    /// "dag 2 av 3 · 1/2 i dag". Dagtelleren utelates for en kur pa en
    /// enkelt dag - "dag 1 av 1" sier ingenting.
    /// </summary>
    public static string Fremdrift(
        int dag, int antallDager, int doserIdag, int gangerPerDag)
        => antallDager > 1
            ? $"dag {dag} av {antallDager} · {doserIdag}/{gangerPerDag} i dag"
            : $"{doserIdag}/{gangerPerDag} i dag";

    /// <summary>
    /// "1 gang daglig i 3 dager" til historikken, eller null for en vanlig
    /// behandling.
    /// </summary>
    public static string? Beskrivelse(int antallDager, int gangerPerDag)
    {
        if (!ErKur(antallDager, gangerPerDag))
        {
            return null;
        }

        var ganger = gangerPerDag == 1 ? "1 gang" : $"{gangerPerDag} ganger";

        return antallDager == 1
            ? $"{ganger} samme dag"
            : $"{ganger} daglig i {antallDager} dager";
    }
}
