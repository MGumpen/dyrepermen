using System.ComponentModel.DataAnnotations;

namespace Dyrepermen.Web.ViewModels;

public sealed class LoggInnVm
{
    [Required(ErrorMessage = "Skriv inn e-postadressen din.")]
    [EmailAddress(ErrorMessage = "Dette ser ikke ut som en e-postadresse.")]
    [Display(Name = "E-post")]
    public string Epost { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv inn passordet ditt.")]
    [DataType(DataType.Password)]
    [Display(Name = "Passord")]
    public string Passord { get; set; } = string.Empty;

    [Display(Name = "Husk meg")]
    public bool HuskMeg { get; set; } = true;
}
