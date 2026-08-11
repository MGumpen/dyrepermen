using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Kobler en bruker til en husstand med en rolle. Erstatter den tidligere
/// kolonnen bruker.husstand_id, som bare kunne holde en verdi.
///
/// Denne tabellen er selve tenant-koblingen og har derfor INGEN query-filter.
/// Et filter her ville gjort det umulig a finne ut hvilke husstander du er
/// med i - som er nettopp det man trenger for a bytte mellom dem.
/// Tilgangen handheves eksplisitt i tjenestene i stedet.
///
/// Se docs/beslutninger/0009-flere-husstander-per-bruker.md
/// </summary>
public sealed class Husstandsmedlemskap
{
    public int Id { get; set; }

    public int HusstandId { get; set; }

    public Husstand Husstand { get; set; } = null!;

    public int BrukerId { get; set; }

    public Bruker Bruker { get; set; } = null!;

    public Husstandsrolle Rolle { get; set; } = Husstandsrolle.Eier;

    public DateOnly OpprettetDato { get; set; }
}
