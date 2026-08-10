using System.ComponentModel.DataAnnotations;

namespace Dyrepermen.Web.ViewModels;

public sealed class NyVektVm
{
    /// <summary>
    /// Kilo med komma som desimalskilletegn. Modellbindingen bruker nb-NO
    /// fordi RequestCultureProviders er tomt - uten det ville en engelsk
    /// nettleser fatt punktum, mens skjemaet forventer komma.
    /// Se plan kapittel 7.3.
    /// </summary>
    [Required(ErrorMessage = "Skriv inn en vekt.")]
    [Range(0.01, 200, ErrorMessage = "Vekten må være mellom 0,01 og 200 kg.")]
    [Display(Name = "Vekt i kilo")]
    public decimal Kilo { get; set; }

    [Required(ErrorMessage = "Velg en dato.")]
    [DataType(DataType.Date)]
    [Display(Name = "Dato")]
    public DateOnly Dato { get; set; } = DateOnly.FromDateTime(DateTime.Now);
}
