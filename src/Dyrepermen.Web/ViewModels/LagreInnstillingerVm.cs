using System.ComponentModel.DataAnnotations;

namespace Dyrepermen.Web.ViewModels;

/// <summary>
/// Kun feltene skjemaet faktisk poster.
///
/// InnstillingerVm inneholder ogsa Husstandsoversikt for visning. Brukes den
/// som parameter til POST-handlingen, behandler ASP.NET Core den
/// ikke-nullbare Oversikt-egenskapen som implisitt pakrevd - og siden
/// skjemaet aldri poster den, blir ModelState ugyldig hver eneste gang.
/// Resultatet er en side som lastes pa nytt uten a lagre og uten a si fra.
///
/// Regelen: visningsmodell og inndatamodell er ikke samme type.
/// </summary>
public sealed class LagreInnstillingerVm
{
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
}
