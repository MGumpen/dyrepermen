using System.ComponentModel.DataAnnotations;

namespace Dyrepermen.Web.ViewModels;

public sealed class NyMedisinVm
{
    [Required(ErrorMessage = "Skriv inn navnet på medisinen.")]
    [StringLength(80, ErrorMessage = "Navnet kan være høyst 80 tegn.")]
    [Display(Name = "Medisin")]
    public string Navn { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv inn doseringen.")]
    [StringLength(40, ErrorMessage = "Doseringen kan være høyst 40 tegn.")]
    [Display(Name = "Dose")]
    public string Dose { get; set; } = string.Empty;

    // Nullbar sa feltet star tomt med plassholder. Som int rendret det "0",
    // og brukeren matte viske ut nullen for hun kunne skrive. Tomt felt
    // tolkes som 0 - altsa ingen fast gjentakelse.
    [Range(0, 8760, ErrorMessage = "Intervallet må være mellom 0 og 8760 timer.")]
    [Display(Name = "Intervall i timer")]
    public int? IntervallTimer { get; set; }

    [Required(ErrorMessage = "Velg en startdato.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fra")]
    public DateOnly StartDato { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    [DataType(DataType.Date)]
    [Display(Name = "Til")]
    public DateOnly? SluttDato { get; set; }
}
