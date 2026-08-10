using Dyrepermen.Domain.Abstractions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Brukerdefinert forregel per dyr.
///
/// Applikasjonen anbefaler ikke formengde - den regner ut regelen brukeren
/// selv har lagt inn. Riktig mengde avhenger av art, rase, alder, fortype,
/// aktivitetsniva og hold, og en innebygd formel ville gitt et tall som ser
/// autoritativt ut uten a ha dekning for det. Se plan kapittel 8.1.
///
/// De to metodene er gjensidig utelukkende, handhevet av ck_forplan_verdi.
/// Kun en plan per dyr kan vaere aktiv, handhevet av ux_forplan_aktiv.
/// </summary>
public sealed class Forplan : IHusstandsbundet
{
    public int Id { get; set; }

    public int DyrId { get; set; }

    public Dyr Dyr { get; set; } = null!;

    public Formetode Metode { get; set; }

    /// <summary>
    /// Tidels prosent: 50 betyr 5,0 %. Holder hele modellen pa heltall.
    /// Kun ved metode Prosent, ellers null.
    /// </summary>
    public int? ProsentTidels { get; set; }

    /// <summary>Kun ved metode Gram, ellers null.</summary>
    public int? GramPerDag { get; set; }

    public int AntallMaltider { get; set; } = 2;

    /// <summary>Navn pa foret.</summary>
    public string? Fornavn { get; set; }

    public string? Notat { get; set; }

    public bool Aktiv { get; set; } = true;

    public DateOnly OpprettetDato { get; set; }
}
