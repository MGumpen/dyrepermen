using System.ComponentModel.DataAnnotations;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Web.ViewModels;

public sealed class KontaktVm
{
    private string _melding = string.Empty;

    // Nullbar, sa et skjema uten valg gir en norsk feilmelding. Som vanlig
    // enum ville standardverdien Sporsmal gatt gjennom uten at noen valgte den.
    [Required(ErrorMessage = "Velg hva henvendelsen gjelder.")]
    [Display(Name = "Hva gjelder det?")]
    public Kontakttype? Type { get; set; }

    /// <summary>
    /// Linjeskift gjores om til \n for valideringen.
    ///
    /// Nettleseren teller et linjeskift som ett tegn i maxlength, men poster
    /// det som \r\n. Uten dette ville en melding pa noyaktig 2 000 tegn med
    /// avsnitt blitt avvist av serveren etter at skjemaet godtok den.
    /// </summary>
    [Required(ErrorMessage = "Skriv en melding.")]
    [StringLength(Kontaktepost.MaksLengde,
        ErrorMessage = "Meldingen kan være høyst 2 000 tegn.")]
    [Display(Name = "Melding")]
    public string Melding
    {
        get => _melding;
        set => _melding = value?.Replace("\r\n", "\n") ?? string.Empty;
    }
}
