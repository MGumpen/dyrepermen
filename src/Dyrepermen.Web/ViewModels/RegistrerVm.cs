using System.ComponentModel.DataAnnotations;

namespace Dyrepermen.Web.ViewModels;

public sealed class RegistrerVm
{
    [Required(ErrorMessage = "Skriv inn en e-postadresse.")]
    [EmailAddress(ErrorMessage = "Dette ser ikke ut som en e-postadresse.")]
    [Display(Name = "E-post")]
    public string Epost { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv inn et navn.")]
    [StringLength(60, ErrorMessage = "Navnet kan være høyst 60 tegn.")]
    // Egenskapen heter Visningsnavn - det er navnet de andre i husstanden
    // ser. Men for den som fyller ut skjemaet er det bare navnet hennes,
    // og "Visningsnavn" er et ord fra systemet, ikke fra virkeligheten.
    [Display(Name = "Navn")]
    public string Visningsnavn { get; set; } = string.Empty;

    /// <summary>
    /// Kun lengden sjekkes her, og den leses fra <see cref="Passordkrav"/>.
    ///
    /// Kravet om stor bokstav sjekkes BEVISST ikke i nettleseren. Identity
    /// bruker char.IsUpper, som godtar Ø og É like godt som A. Et regulaert
    /// uttrykk her matte gjentatt den regelen i JavaScript, og et
    /// "[A-Z]"-monster ville avvist passord serveren gjerne tar imot - altsa
    /// samme klasse feil som den vi retter: klienten strengere enn serveren,
    /// uten at noen oppdager det.
    ///
    /// Serveren avgjor. Skjemaet forteller regelen i klartekst, og
    /// feilmeldingen fra Identity er presis nar den forst kommer.
    /// </summary>
    [Required(ErrorMessage = "Velg et passord.")]
    [StringLength(100, MinimumLength = Passordkrav.MinLengde,
        ErrorMessage = Passordkrav.ForKort)]
    [DataType(DataType.Password)]
    [Display(Name = "Passord")]
    public string Passord { get; set; } = string.Empty;

    [Required(ErrorMessage = "Gjenta passordet.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Passord), ErrorMessage = "Passordene er ikke like.")]
    [Display(Name = "Gjenta passord")]
    public string BekreftPassord { get; set; } = string.Empty;
}
