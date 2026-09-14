using System.ComponentModel.DataAnnotations;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Web.ViewModels;

public class NyttDyrVm : IValidatableObject
{
    [Required(ErrorMessage = "Gi dyret et navn.")]
    [StringLength(60, ErrorMessage = "Navnet kan være høyst 60 tegn.")]
    [Display(Name = "Navn")]
    public string Navn { get; set; } = string.Empty;

    [Display(Name = "Art")]
    public Art Art { get; set; } = Art.Hund;

    [Display(Name = "Kjønn")]
    public Kjonn Kjonn { get; set; } = Kjonn.Tispe;

    [StringLength(80, ErrorMessage = "Rasen kan være høyst 80 tegn.")]
    [Display(Name = "Rase")]
    public string? Rase { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fødselsdato")]
    public DateOnly? Fodselsdato { get; set; }

    // Tom verdi er lov. RegularExpression hopper over null og tom streng -
    // kun Required krever verdi.
    [RegularExpression(@"^\d{15}$",
        ErrorMessage = "Chipnummeret må være nøyaktig 15 siffer.")]
    [Display(Name = "Chipnummer")]
    public string? ChipNr { get; set; }

    [StringLength(20, ErrorMessage = "Regnummeret kan være høyst 20 tegn.")]
    [Display(Name = "NKK-regnummer")]
    public string? RegNrNkk { get; set; }

    [Display(Name = "Kastrert")]
    public bool Kastrert { get; set; }

    /// <summary>
    /// NKK-registeret er Norsk Kennel Klub - det gjelder hund. En katt kan
    /// ikke ha regnummer derfra, og feltet skjules for katter i skjemaet.
    /// Sjekken her fanger den som poster likevel.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (Art != Art.Hund && !string.IsNullOrWhiteSpace(RegNrNkk))
        {
            yield return new ValidationResult(
                "NKK-regnummer gjelder bare hunder.", [nameof(RegNrNkk)]);
        }
    }
}
