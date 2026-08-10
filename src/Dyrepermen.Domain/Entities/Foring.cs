using Dyrepermen.Domain.Abstractions;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Logg over gitte maltider.
///
/// <see cref="Tidspunkt"/> settes av tjenesten pa serveren, aldri av klienten.
/// Det kan korrigeres i etterkant gjennom en egen redigeringsvisning - glemmer
/// man a huke av til man kommer hjem, er automatikken feil og ma kunne
/// overstyres. Se plan kapittel 8.2.
/// </summary>
public sealed class Foring : IHusstandsbundet
{
    public int Id { get; set; }

    public int DyrId { get; set; }

    public Dyr Dyr { get; set; } = null!;

    /// <summary>Lagres i UTC. Konverteres til Europe/Oslo i visningslaget.</summary>
    public DateTimeOffset Tidspunkt { get; set; }

    /// <summary>Valgfri - kan hukes av uten mengde.</summary>
    public int? MengdeGram { get; set; }

    public int? GittAvBrukerId { get; set; }

    public Bruker? GittAv { get; set; }

    public string? Kommentar { get; set; }
}
