using System.ComponentModel.DataAnnotations;

namespace Dyrepermen.Web.ViewModels;

public sealed class NyVektVm
{
    /// <summary>
    /// Nullbar med vilje. En ikke-nullbar decimal starter pa 0, og da star
    /// det "0" i feltet for brukeren har skrevet noe - et tall ingen har
    /// ment. Nullbar gir tomt felt og lar plassholderen vises.
    ///
    /// Kilo med komma som desimalskilletegn. Modellbindingen bruker nb-NO
    /// fordi RequestCultureProviders er tomt. Klientvalideringen ma
    /// overstyres separat, se wwwroot/js/norsk-validering.js.
    /// </summary>
    [Required(ErrorMessage = "Skriv inn en vekt.")]
    [Range(0.01, 200, ErrorMessage = "Vekten må være mellom 0,01 og 200 kg.")]
    [Display(Name = "Vekt i kilo")]
    public decimal? Kilo { get; set; }

    [Required(ErrorMessage = "Velg en dato.")]
    [DataType(DataType.Date)]
    [Display(Name = "Dato")]
    public DateOnly Dato { get; set; } = DateOnly.FromDateTime(DateTime.Now);
}
