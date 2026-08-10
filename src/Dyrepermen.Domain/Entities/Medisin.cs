using Dyrepermen.Domain.Abstractions;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Aggregatrot for doselogg. Neste dose beregnes som siste dose pluss
/// <see cref="IntervallTimer"/> - medisiner gjentas per time, ikke per dato.
/// </summary>
public sealed class Medisin : IHusstandsbundet
{
    public int Id { get; set; }

    public int DyrId { get; set; }

    public Dyr Dyr { get; set; } = null!;

    public string Navn { get; set; } = null!;

    /// <summary>Fritekst, for eksempel "1/2 tablett".</summary>
    public string Dose { get; set; } = null!;

    /// <summary>0 betyr ved behov.</summary>
    public int IntervallTimer { get; set; }

    public DateOnly StartDato { get; set; }

    /// <summary>Null betyr pagaende.</summary>
    public DateOnly? SluttDato { get; set; }

    public ICollection<Dose> Doser { get; set; } = new List<Dose>();
}
