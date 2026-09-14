using Dyrepermen.Domain.Abstractions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Vaksine, ormekur, flattmiddel, kloklipp eller tannrens.
/// <see cref="NesteDato"/> driver paminnelsene.
/// </summary>
public sealed class Behandling : IHusstandsbundet
{
    public int Id { get; set; }

    public int DyrId { get; set; }

    public Dyr Dyr { get; set; } = null!;

    public BehandlingType Type { get; set; }

    public string? Preparat { get; set; }

    public DateOnly Dato { get; set; }

    public DateOnly? NesteDato { get; set; }

    public string? Notat { get; set; }
}
