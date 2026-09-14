using Dyrepermen.Domain.Abstractions;
using Dyrepermen.Domain.Enums;

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

    /// <summary>
    /// Maltid eller godbit. Kun maltider teller mot "maltid 2 av 3".
    /// </summary>
    public Foringstype Type { get; set; }

    /// <summary>Valgfri - kan hukes av uten mengde.</summary>
    public int? MengdeGram { get; set; }

    /// <summary>
    /// Hva som ble gitt: "Royal Canin Maxi", "Tyggebein". Fritekst, ikke
    /// fremmednokkel - et eget forregister ville krevd vedlikehold for a gi
    /// et navn vi like gjerne kan skrive. Tidligere verdier tilbys som
    /// forslag, sa stavematen holder seg stabil av seg selv.
    /// </summary>
    public string? Fornavn { get; set; }

    public int? GittAvBrukerId { get; set; }

    public Bruker? GittAv { get; set; }

    public string? Kommentar { get; set; }
}
