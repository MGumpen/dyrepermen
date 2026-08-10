using System.ComponentModel.DataAnnotations;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Web.ViewModels;

public sealed class NyForplanVm : IValidatableObject
{
    [Display(Name = "Metode")]
    public Formetode Metode { get; set; } = Formetode.Gram;

    /// <summary>
    /// Prosent av kroppsvekt, slik brukeren skriver det: 5,0 betyr 5 %.
    /// Lagres som tidels prosent, altsa 50. Nullbar sa feltet starter tomt.
    /// </summary>
    [Range(0.1, 30, ErrorMessage = "Prosenten må være mellom 0,1 og 30.")]
    [Display(Name = "Prosent av kroppsvekt")]
    public decimal? Prosent { get; set; }

    [Range(1, 20000, ErrorMessage = "Mengden må være mellom 1 og 20000 gram.")]
    [Display(Name = "Gram per dag")]
    public int? GramPerDag { get; set; }

    [Range(1, 6, ErrorMessage = "Antall måltider må være mellom 1 og 6.")]
    [Display(Name = "Antall måltider per dag")]
    public int AntallMaltider { get; set; } = 2;

    [StringLength(80, ErrorMessage = "Navnet kan være høyst 80 tegn.")]
    [Display(Name = "Fôrets navn")]
    public string? Fornavn { get; set; }

    [StringLength(300, ErrorMessage = "Notatet kan være høyst 300 tegn.")]
    [Display(Name = "Notat")]
    public string? Notat { get; set; }

    /// <summary>
    /// De to metodene er gjensidig utelukkende. Uten denne sjekken slar
    /// ck_forplan_verdi inn som en DbUpdateException i stedet for en
    /// forstaelig melding ved feltet.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (Metode == Formetode.Prosent && Prosent is null)
        {
            yield return new ValidationResult(
                "Skriv inn en prosentsats.", [nameof(Prosent)]);
        }

        if (Metode == Formetode.Gram && GramPerDag is null)
        {
            yield return new ValidationResult(
                "Skriv inn antall gram per dag.", [nameof(GramPerDag)]);
        }
    }
}
