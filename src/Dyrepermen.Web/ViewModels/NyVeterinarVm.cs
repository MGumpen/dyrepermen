using System.ComponentModel.DataAnnotations;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Web.ViewModels;

/// <summary>
/// Kun feltene skjemaet poster. Visningsmodell og inndatamodell er ikke
/// samme type - se LagreInnstillingerVm for hvorfor.
/// </summary>
public sealed class NyVeterinarVm
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Stedet må ha et navn.")]
    [StringLength(100, ErrorMessage = "Navnet kan være høyst 100 tegn.")]
    [Display(Name = "Navn")]
    public string Navn { get; set; } = string.Empty;

    [Display(Name = "Type")]
    public Veterinartype Type { get; set; }

    [StringLength(20, MinimumLength = 3,
        ErrorMessage = "Telefonnummeret må være mellom 3 og 20 tegn.")]
    [Display(Name = "Telefon")]
    public string? Telefon { get; set; }

    [StringLength(200, ErrorMessage = "Adressen kan være høyst 200 tegn.")]
    [Display(Name = "Adresse")]
    public string? Adresse { get; set; }

    [StringLength(200)]
    [Display(Name = "Nettside")]
    public string? Nettside { get; set; }

    [EmailAddress(ErrorMessage = "Skriv en gyldig e-postadresse.")]
    [StringLength(200)]
    [Display(Name = "E-post")]
    public string? Epost { get; set; }

    [StringLength(200)]
    [Display(Name = "Åpningstider")]
    public string? Apningstider { get; set; }

    [StringLength(500)]
    [Display(Name = "Notat")]
    public string? Notat { get; set; }
}
