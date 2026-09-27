using System.ComponentModel.DataAnnotations;

namespace Dyrepermen.Web.ViewModels;

public sealed class NyttVetbesokVm
{
    public const int ArsakMaks = 200;

    public int? Id { get; set; }

    /// <summary>
    /// Besoket timen er kontrollen for. Satt nar skjemaet ble apnet fra
    /// "Bestill time" pa en kontroll i "Forfaller snart", og fjerner
    /// paminnelsen nar timen lagres. Kun for nye timer.
    /// </summary>
    public int? KontrollForBesokId { get; set; }

    /// <summary>
    /// Skjemaet ble apnet for a registrere resultatet av et besok som er
    /// gjennomfort. Endrer bare overskrift og bekreftelse, og bares med
    /// gjennom en valideringsfeil i et skjult felt.
    /// </summary>
    public bool Gjennomfort { get; set; }

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
    [StringLength(ArsakMaks, ErrorMessage = "Årsaken kan være høyst 200 tegn.")]
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
