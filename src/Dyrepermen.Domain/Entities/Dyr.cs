using Dyrepermen.Domain.Abstractions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Aggregatrot for alt som logges om ett dyr.
///
/// Dyr slettes ikke, de deaktiveres ved <see cref="Aktiv"/> = false. Historikk
/// om et dyr som er gatt bort skal bevares. Se plan kapittel 5.2.
///
/// <see cref="ChipNr"/> og <see cref="RegNrNkk"/> er globalt unike, ikke unike
/// per husstand - chipnummer er unike pa verdensbasis. Tom streng ma
/// normaliseres til null for lagring, ellers kolliderer to dyr uten chipnummer.
/// Se plan kapittel 5.3.
/// </summary>
public sealed class Dyr : IHusstandsbundet
{
    public int Id { get; set; }

    public int HusstandId { get; set; }

    public Husstand Husstand { get; set; } = null!;

    public string Navn { get; set; } = null!;

    public Art Art { get; set; }

    public string? Rase { get; set; }

    public Kjonn Kjonn { get; set; }

    public DateOnly? Fodselsdato { get; set; }

    /// <summary>Globalt unikt. Norske chip starter pa 578. Alltid 15 tegn.</summary>
    public string? ChipNr { get; set; }

    /// <summary>Globalt unikt, sammenlignet uten hensyn til store bokstaver.</summary>
    public string? RegNrNkk { get; set; }

    public bool Kastrert { get; set; }

    public string? BildeFilnavn { get; set; }

    /// <summary>Funksjonsbryter. Arves fra husstandens standard ved opprettelse.</summary>
    public bool ForingsloggAktiv { get; set; }

    /// <summary>Funksjonsbryter. Uavhengig av <see cref="ForingsloggAktiv"/>.</summary>
    public bool ForplanAktiv { get; set; } = true;

    /// <summary>Settes false i stedet for sletting.</summary>
    public bool Aktiv { get; set; } = true;

    public ICollection<Vekt> Vekter { get; set; } = new List<Vekt>();

    public ICollection<Behandling> Behandlinger { get; set; } = new List<Behandling>();

    public ICollection<Medisin> Medisiner { get; set; } = new List<Medisin>();

    public ICollection<Foring> Foringer { get; set; } = new List<Foring>();

    public ICollection<Vetbesok> Vetbesok { get; set; } = new List<Vetbesok>();

    public ICollection<Forsikring> Forsikringer { get; set; } = new List<Forsikring>();

    public ICollection<Dokument> Dokumenter { get; set; } = new List<Dokument>();

    public ICollection<Forplan> Forplaner { get; set; } = new List<Forplan>();
}
