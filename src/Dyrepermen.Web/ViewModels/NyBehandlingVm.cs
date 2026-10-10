using System.ComponentModel.DataAnnotations;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Web.ViewModels;

public sealed class NyBehandlingVm : IValidatableObject
{
    [Display(Name = "Type")]
    public BehandlingType Type { get; set; } = BehandlingType.Vaksine;

    [StringLength(80, ErrorMessage = "Preparatet kan være høyst 80 tegn.")]
    [Display(Name = "Preparat")]
    public string? Preparat { get; set; }

    /// <summary>Fritekst, valgfri. "12,5 mg/125 mg" eller "2,5 ml".</summary>
    [StringLength(40, ErrorMessage = "Styrken kan være høyst 40 tegn.")]
    [Display(Name = "Styrke")]
    public string? Styrke { get; set; }

    [Required(ErrorMessage = "Velg en dato.")]
    [DataType(DataType.Date)]
    [Display(Name = "Dato")]
    public DateOnly Dato { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Driver paminnelsene pa dashbordet. Valgfri.</summary>
    [DataType(DataType.Date)]
    [Display(Name = "Neste gang")]
    public DateOnly? NesteDato { get; set; }

    /// <summary>
    /// Nullbar, ikke 1 - samme grep som antall maltider i forplanen. Tomt
    /// felt tolkes som 1 i controlleren, og 1 star som plassholder, sa et
    /// tall kan skrives inn uten at et annet ma viskes ut forst.
    ///
    /// Taket er ikke i databasen, som bare krever minst 1. Det er her for a
    /// fange en tastefeil: 300 dager med 3 doser er 900 knappetrykk.
    /// </summary>
    [Range(1, MaksDager, ErrorMessage = "Antall dager må være mellom 1 og 365.")]
    [Display(Name = "Antall dager")]
    public int? AntallDager { get; set; }

    [Range(1, MaksGangerPerDag, ErrorMessage = "Ganger per dag må være mellom 1 og 24.")]
    [Display(Name = "Ganger per dag")]
    public int? GangerPerDag { get; set; }

    public const int MaksDager = 365;
    public const int MaksGangerPerDag = 24;

    [StringLength(500, ErrorMessage = "Notatet kan være høyst 500 tegn.")]
    [Display(Name = "Notat")]
    public string? Notat { get; set; }

    /// <summary>
    /// "Annet" uten beskrivelse gir en rad som bare sier "Annet" og en dato.
    /// Den forteller ingenting seks maneder senere, sa feltet kreves her.
    /// De faste typene baerer sitt eget navn og klarer seg uten.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (Type == BehandlingType.Annet && string.IsNullOrWhiteSpace(Preparat))
        {
            yield return new ValidationResult(
                "Skriv hva behandlingen var.", [nameof(Preparat)]);
        }
    }
}
