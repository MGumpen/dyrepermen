using Dyrepermen.Domain.Abstractions;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Vekt lagres i gram som heltall. 27,4 kg blir 27400. All aritmetikk blir
/// eksakt, og formatering skjer i visningslaget. Se plan kapittel 5.2.
/// </summary>
public sealed class Vekt : IHusstandsbundet
{
    public int Id { get; set; }

    public int DyrId { get; set; }

    public Dyr Dyr { get; set; } = null!;

    public int VektGram { get; set; }

    public DateOnly Dato { get; set; }

    /// <summary>
    /// Nullbar fordi brukeren kan vaere slettet. Sletting av en bruker skal
    /// ikke ta med seg hundens vekthistorikk. Se plan kapittel 12.5.
    /// </summary>
    public int? RegistrertAvBrukerId { get; set; }

    public Bruker? RegistrertAv { get; set; }
}
