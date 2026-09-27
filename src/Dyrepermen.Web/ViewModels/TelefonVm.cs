using System.ComponentModel.DataAnnotations;

namespace Dyrepermen.Web.ViewModels;

public sealed class TelefonVm
{
    // Sifre, mellomrom, bindestrek, parenteser og en innledende +. Formatet
    // tvinges ikke: "+47 412 34 567" og "41234567" er begge riktige.
    [StringLength(20, MinimumLength = 3,
        ErrorMessage = "Telefonnummeret må være mellom 3 og 20 tegn.")]
    [RegularExpression(@"^\+?[0-9 ()\-]+$",
        ErrorMessage = "Telefonnummeret kan bare inneholde sifre, mellomrom og +.")]
    [Display(Name = "Telefon")]
    public string? Nummer { get; set; }
}
