using System.ComponentModel.DataAnnotations;

namespace Dyrepermen.Web.ViewModels;

public sealed class NyttVetbesokVm
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Velg hvilket dyr timen gjelder.")]
    [Display(Name = "Dyr")]
    public int DyrId { get; set; }

    [Display(Name = "Sted")]
    public int? VeterinarId { get; set; }

    [StringLength(100)]
    [Display(Name = "Annet sted")]
    public string? Klinikk { get; set; }

    [Required(ErrorMessage = "Timen må ha en dato.")]
    [DataType(DataType.Date)]
    [Display(Name = "Dato")]
    public DateOnly Dato { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    [DataType(DataType.Time)]
    [Display(Name = "Klokkeslett")]
    public TimeOnly? Klokkeslett { get; set; }

    [Required(ErrorMessage = "Skriv hva timen gjelder.")]
    [StringLength(200, ErrorMessage = "Årsaken kan være høyst 200 tegn.")]
    [Display(Name = "Årsak")]
    public string Arsak { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Diagnose")]
    public string? Diagnose { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Beløpet må være mellom 0 og 1 000 000.")]
    [Display(Name = "Pris i kroner")]
    public int? KostnadKr { get; set; }

    [Display(Name = "Forsikring brukt")]
    public bool ForsikringKrevd { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "Beløpet må være mellom 0 og 1 000 000.")]
    [Display(Name = "Refundert i kroner")]
    public int? RefundertKr { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Neste kontroll")]
    public DateOnly? NesteKontrollDato { get; set; }

    [StringLength(500)]
    [Display(Name = "Notat")]
    public string? Notat { get; set; }
}
