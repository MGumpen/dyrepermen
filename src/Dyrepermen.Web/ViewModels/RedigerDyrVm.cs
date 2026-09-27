using System.ComponentModel.DataAnnotations;

namespace Dyrepermen.Web.ViewModels;

/// <summary>
/// Arver feltene fra opprettelse og legger til funksjonsbryterne.
/// Bryterne betjenes her, pa dyrets egen side - ikke pa en separat
/// innstillingsside. Se plan kapittel 8.2.
/// </summary>
public sealed class RedigerDyrVm : NyttDyrVm
{
    public int Id { get; set; }

    /// <summary>
    /// Kun til visning. Bildet lastes opp og fjernes med egne handlinger, ikke
    /// med dette skjemaet - sa en valideringsfeil i navnet ikke koster bildet.
    /// </summary>
    public int? ProfilbildeId { get; set; }

    [Display(Name = "Før fôringslogg for dette dyret")]
    public bool ForingsloggAktiv { get; set; }

    [Display(Name = "Bruk fôrplan for dette dyret")]
    public bool ForplanAktiv { get; set; }
}
