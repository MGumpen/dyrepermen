using System.ComponentModel.DataAnnotations;

namespace Dyrepermen.Web.ViewModels;

public sealed class RegistrerVm
{
    [Required(ErrorMessage = "Skriv inn en e-postadresse.")]
    [EmailAddress(ErrorMessage = "Dette ser ikke ut som en e-postadresse.")]
    [Display(Name = "E-post")]
    public string Epost { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv inn et navn.")]
    [StringLength(60, ErrorMessage = "Navnet kan være høyst 60 tegn.")]
    [Display(Name = "Visningsnavn")]
    public string Visningsnavn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Velg et passord.")]
    [StringLength(100, MinimumLength = 10,
        ErrorMessage = "Passordet må være minst 10 tegn.")]
    [DataType(DataType.Password)]
    [Display(Name = "Passord")]
    public string Passord { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Passord), ErrorMessage = "Passordene er ikke like.")]
    [Display(Name = "Gjenta passord")]
    public string BekreftPassord { get; set; } = string.Empty;
}
