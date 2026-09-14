using System.ComponentModel.DataAnnotations;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Web.ViewModels;

/// <summary>
/// Egen inn-modell. Alle felt er nullbare med vilje: en ikke-nullbar
/// string er implisitt pakrevd, og da blir ModelState ugyldig for et felt
/// skjemaet aldri sendte. Det var akkurat den feilen som gjorde at
/// innstillinger ikke lot seg lagre.
/// </summary>
public sealed class RegistrerForingVm
{
    public Foringstype Type { get; set; }

    [Range(1, 5000, ErrorMessage = "Mengden må være mellom 1 og 5000 gram.")]
    [Display(Name = "Mengde i gram")]
    public int? MengdeGram { get; set; }

    [StringLength(80, ErrorMessage = "Navnet kan være høyst 80 tegn.")]
    public string? Fornavn { get; set; }

    [StringLength(200, ErrorMessage = "Kommentaren kan være høyst 200 tegn.")]
    public string? Kommentar { get; set; }
}
