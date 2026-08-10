using System.ComponentModel.DataAnnotations;

namespace Dyrepermen.Web.ViewModels;

public sealed class SlettKontoVm
{
    [Required(ErrorMessage = "Skriv inn passordet ditt.")]
    [DataType(DataType.Password)]
    [Display(Name = "Passord")]
    public string Passord { get; set; } = string.Empty;

    /// <summary>
    /// Eksplisitt bekreftelse, ikke bare "Er du sikker?". Brukeren ma skrive
    /// ordet selv. Se plan kapittel 12.5.
    /// </summary>
    [Required(ErrorMessage = "Skriv SLETT for å bekrefte.")]
    [RegularExpression("^SLETT$", ErrorMessage = "Skriv SLETT med store bokstaver.")]
    [Display(Name = "Skriv SLETT")]
    public string Bekreftelse { get; set; } = string.Empty;

    [Display(Name = "Jeg forstår at husstanden og alle data slettes")]
    public bool BekrefterHusstandsletting { get; set; }

    public bool ErSisteMedlem { get; set; }

    public int AntallDyr { get; set; }
}
