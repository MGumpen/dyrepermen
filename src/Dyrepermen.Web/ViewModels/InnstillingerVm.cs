using System.ComponentModel.DataAnnotations;
using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

public sealed class InnstillingerVm
{
    public Husstandsoversikt Oversikt { get; set; } = null!;

    [Required(ErrorMessage = "Husstanden må ha et navn.")]
    [StringLength(80, ErrorMessage = "Navnet kan være høyst 80 tegn.")]
    [Display(Name = "Navn på husstanden")]
    public string Husstandsnavn { get; set; } = string.Empty;

    [Display(Name = "Før fôringslogg på nye dyr")]
    public bool ForingsloggStandard { get; set; }

    [Display(Name = "Bruk fôrplan på nye dyr")]
    public bool ForplanStandard { get; set; }

    [Display(Name = "Send varsler på e-post")]
    public bool VarslerAktiv { get; set; }

    [Display(Name = "Vis godbitknapp")]
    public bool GodbitloggAktiv { get; set; }

    [EmailAddress(ErrorMessage = "Dette ser ikke ut som en e-postadresse.")]
    [Display(Name = "E-postadresse")]
    public string? NyttMedlemEpost { get; set; }
}
