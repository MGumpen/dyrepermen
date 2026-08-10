using Dyrepermen.Domain.Abstractions;

namespace Dyrepermen.Domain.Entities;

/// <summary>Veterinaerbesok med kostnad i hele kroner.</summary>
public sealed class Vetbesok : IHusstandsbundet
{
    public int Id { get; set; }

    public int DyrId { get; set; }

    public Dyr Dyr { get; set; } = null!;

    public DateOnly Dato { get; set; }

    public string? Klinikk { get; set; }

    public string Arsak { get; set; } = null!;

    public string? Diagnose { get; set; }

    public int KostnadKr { get; set; }

    public bool ForsikringKrevd { get; set; }
}
