using Dyrepermen.Domain.Abstractions;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Logg over faktisk gitt medisin. <see cref="GittAvBrukerId"/> er hele
/// poenget med to brukere pa samme husstand - den hindrer dobbeltdosering.
/// </summary>
public sealed class Dose : IHusstandsbundet
{
    public int Id { get; set; }

    public int MedisinId { get; set; }

    public Medisin Medisin { get; set; } = null!;

    /// <summary>Lagres i UTC. Konverteres til Europe/Oslo i visningslaget.</summary>
    public DateTimeOffset GittTid { get; set; }

    public int? GittAvBrukerId { get; set; }

    public Bruker? GittAv { get; set; }
}
