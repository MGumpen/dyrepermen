using Dyrepermen.Domain.Abstractions;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// En dose gitt i en kur som gar over flere dager eller gis flere ganger per
/// dag. Samme form som <see cref="Dose"/> for medisin: hvem og nar, ikke
/// mer. Hvor mange som er gitt i dag, telles fra norsk midnatt.
/// </summary>
public sealed class BehandlingDose : IHusstandsbundet
{
    public int Id { get; set; }

    public int BehandlingId { get; set; }

    public Behandling Behandling { get; set; } = null!;

    /// <summary>Lagres i UTC. Settes pa serveren, aldri fra klienten.</summary>
    public DateTimeOffset GittTid { get; set; }

    public int? GittAvBrukerId { get; set; }

    public Bruker? GittAv { get; set; }
}
