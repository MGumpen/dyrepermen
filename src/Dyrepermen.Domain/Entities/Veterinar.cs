using Dyrepermen.Domain.Abstractions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Et sted a ringe. Horer til husstanden, ikke til det enkelte dyret - man
/// bruker samme klinikk til hunden og katten, og vakta er den samme uansett
/// hvem som er syk.
///
/// Grunnen til at dette er en egen tabell og ikke fritekst pa hvert besok:
/// nummeret til vakta skal vaere til a finne klokka to om natten, uten a
/// lete gjennom gamle besok etter det.
/// </summary>
public sealed class Veterinar : IHusstandsbundet
{
    public int Id { get; set; }

    public int HusstandId { get; set; }

    public Husstand Husstand { get; set; } = null!;

    public string Navn { get; set; } = null!;

    public Veterinartype Type { get; set; }

    /// <summary>
    /// Lagres som varchar med lengdevilkar, ikke char. char(15) blank-padder
    /// i PostgreSQL, og da blir "22 12 34 56" til "22 12 34 56    " - som
    /// ikke lenger er lik seg selv ved sammenligning.
    ///
    /// Ingen formatvalidering utover lengde. Utenlandske numre, kortnumre og
    /// nummer med landkode skal alle kunne skrives inn.
    /// </summary>
    public string? Telefon { get; set; }

    public string? Adresse { get; set; }

    public string? Nettside { get; set; }

    public string? Epost { get; set; }

    // Apningstid per ukedag, som fritekst. En tom dag betyr stengt.
    //
    // Sju kolonner og ikke en samletekst: da kan grensesnittet vise kun de
    // dagene som faktisk er fylt ut, og brukeren slipper a formatere selv.
    // Fritekst per dag og ikke fra/til-klokkeslett fordi "10-14, 16-20" og
    // "Dognapent" begge er vanlige, og ingen av dem er to tidspunkter.
    public string? ApentMandag { get; set; }

    public string? ApentTirsdag { get; set; }

    public string? ApentOnsdag { get; set; }

    public string? ApentTorsdag { get; set; }

    public string? ApentFredag { get; set; }

    public string? ApentLordag { get; set; }

    public string? ApentSondag { get; set; }

    public string? Notat { get; set; }

    public DateOnly OpprettetDato { get; set; }

    /// <summary>
    /// Besok som peker hit. Slettes stedet, beholdes besokene - historikken
    /// skal ikke forsvinne fordi klinikken byttet navn.
    /// </summary>
    public ICollection<Vetbesok> Besok { get; set; } = new List<Vetbesok>();
}
