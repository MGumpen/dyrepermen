using Dyrepermen.Domain.Abstractions;

namespace Dyrepermen.Domain.Entities;

/// <summary><see cref="FornyesDato"/> driver paminnelsen.</summary>
public sealed class Forsikring : IHusstandsbundet
{
    public int Id { get; set; }

    public int DyrId { get; set; }

    public Dyr Dyr { get; set; } = null!;

    public string Selskap { get; set; } = null!;

    public string PoliseNr { get; set; } = null!;

    public int ArspremieKr { get; set; }

    public int Egenandel { get; set; }

    public DateOnly? FornyesDato { get; set; }
}
