using Dyrepermen.Domain.Abstractions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Vaksine, ormekur, flattmiddel, kloklipp eller tannrens.
/// <see cref="NesteDato"/> driver paminnelsene.
///
/// En behandling kan vaere en kur over flere dager, med flere doser per dag -
/// ormekur en gang daglig i tre dager. <see cref="Dato"/> er da forste dag,
/// og dosene logges i <see cref="Doser"/>. En behandling med en dag og en
/// gang regnes som gitt nar den registreres, og far ingen doser.
/// Se ADR 0019.
/// </summary>
public sealed class Behandling : IHusstandsbundet
{
    public int Id { get; set; }

    public int DyrId { get; set; }

    public Dyr Dyr { get; set; } = null!;

    public BehandlingType Type { get; set; }

    public string? Preparat { get; set; }

    /// <summary>
    /// Fritekst, for eksempel "12,5 mg/125 mg" eller "2,5 ml". Ikke en del
    /// av hva som folger opp en paminnelse - en valp far sterkere tablett
    /// etter hvert som den vokser, men det er fortsatt samme ormekur.
    /// Se ADR 0020.
    /// </summary>
    public string? Styrke { get; set; }

    public DateOnly Dato { get; set; }

    public DateOnly? NesteDato { get; set; }

    public string? Notat { get; set; }

    /// <summary>
    /// Kurens lengde, forste dag medregnet. Standarden settes her og ikke
    /// bare i databasen: EF utelater en kolonne som har CLR-standarden 0, og
    /// da ville databasens standard skjult en feil i stedet for a avvise den.
    /// </summary>
    public int AntallDager { get; set; } = 1;

    public int GangerPerDag { get; set; } = 1;

    public ICollection<BehandlingDose> Doser { get; set; } = new List<BehandlingDose>();
}
