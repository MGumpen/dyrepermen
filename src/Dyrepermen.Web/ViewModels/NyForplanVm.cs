using System.ComponentModel.DataAnnotations;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Web.ViewModels;

public sealed class NyForplanVm : IValidatableObject
{
    /// <summary>
    /// Antall rader i fortabellen. Fast antall framfor en "legg til
    /// rad"-knapp: en forpose har fire-fem trinn, og et skjema som virker
    /// uten javascript er verdt mer enn de radene ingen fyller ut.
    /// </summary>
    public const int Trinnplasser = 8;

    [Display(Name = "Metode")]
    public Formetode Metode { get; set; } = Formetode.Gram;

    /// <summary>
    /// Prosent av kroppsvekt, slik brukeren skriver det: 5,0 betyr 5 %.
    /// Lagres som tidels prosent, altsa 50. Nullbar sa feltet starter tomt.
    ///
    /// Brukes av prosentmetoden, og av vektdelen nar en tabellplan blander to
    /// for. Det er den samme regelen, og to felter for samme tall ville
    /// sprikt.
    /// </summary>
    [Range(0.1, 30, ErrorMessage = "Prosenten må være mellom 0,1 og 30.")]
    [Display(Name = "Prosent av kroppsvekt")]
    public decimal? Prosent { get; set; }

    [Range(1, 20000, ErrorMessage = "Mengden må være mellom 1 og 20 000 gram.")]
    [Display(Name = "Gram per dag")]
    public int? GramPerDag { get; set; }

    /// <summary>
    /// Skrur pa feltene for det andre foret. Uten bryteren matte alle som
    /// bare vil skrive av en forpose, ogsa ta stilling til en blanding de
    /// ikke har.
    /// </summary>
    [Display(Name = "Bland med et fôr som måles etter vekt")]
    public bool BlandToFor { get; set; }

    /// <summary>
    /// Hvor stor andel som males etter vekt. Resten males etter tabellen, og
    /// derfor finnes det ikke noe felt for den andre andelen - to felter som
    /// ma summere seg til hundre er ett felt for mye.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Andelen må være mellom 1 og 100 prosent.")]
    [Display(Name = "Andel som måles etter vekt (%)")]
    public int? VektdelAndel { get; set; }

    /// <summary>
    /// Nullbar, ikke 2. Tallet 2 er en fornuftig standard, men som ferdig
    /// utfylt verdi ma den viskes ut for man kan skrive 3 - og det gjelder
    /// hver gang. Na star 2 som plassholder i stedet, og tomt felt tolkes
    /// som 2 i controlleren. Brukeren far samme standard uten a matte
    /// fjerne noe.
    /// </summary>
    [Range(1, 6, ErrorMessage = "Antall måltider må være mellom 1 og 6.")]
    [Display(Name = "Antall måltider per dag")]
    public int? AntallMaltider { get; set; }

    [StringLength(80, ErrorMessage = "Navnet kan være høyst 80 tegn.")]
    [Display(Name = "Fôrets navn")]
    public string? Fornavn { get; set; }

    [StringLength(80, ErrorMessage = "Navnet kan være høyst 80 tegn.")]
    [Display(Name = "Fôrets navn")]
    public string? FornavnAlder { get; set; }

    /// <summary>Sant nar planen faktisk skal blande to fortyper.</summary>
    public bool Blander => Metode == Formetode.Tabell && BlandToFor;

    [StringLength(300, ErrorMessage = "Notatet kan være høyst 300 tegn.")]
    [Display(Name = "Notat")]
    public string? Notat { get; set; }

    /// <summary>Fortabellen. Tomme rader ignoreres.</summary>
    public List<TrinnradVm> Trinn { get; set; } =
        [.. Enumerable.Range(0, Trinnplasser).Select(_ => new TrinnradVm())];

    /// <summary>
    /// Metodene er gjensidig utelukkende. Uten disse sjekkene slar
    /// ck_forplan_verdi inn som en DbUpdateException i stedet for en
    /// forstaelig melding ved feltet.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (Metode == Formetode.Gram)
        {
            if (GramPerDag is null)
            {
                yield return new ValidationResult(
                    "Skriv inn antall gram per dag.", [nameof(GramPerDag)]);
            }

            yield break;
        }

        if (Metode == Formetode.Prosent)
        {
            if (Prosent is null)
            {
                yield return new ValidationResult(
                    "Skriv inn en prosentsats.", [nameof(Prosent)]);
            }

            yield break;
        }

        foreach (var feil in Tabellfeil())
        {
            yield return feil;
        }
    }

    private IEnumerable<ValidationResult> Tabellfeil()
    {
        if (BlandToFor)
        {
            if (VektdelAndel is null)
            {
                yield return new ValidationResult(
                    "Skriv inn hvor stor andel som måles etter vekt.",
                    [nameof(VektdelAndel)]);
            }

            if (Prosent is null)
            {
                yield return new ValidationResult(
                    "Skriv inn hvor mange prosent av kroppsvekten det fôret skal utgjøre.",
                    [nameof(Prosent)]);
            }
        }

        // En halvt utfylt rad er en skrivefeil, ikke en tom rad. Ignorerte vi
        // den, ville brukeren sittet igjen med en tabell som mangler et trinn
        // hun trodde hun hadde lagt inn.
        if (Trinn.Any(t => !t.ErTom && !t.ErUtfylt))
        {
            yield return new ValidationResult(
                "Hver rad i tabellen må ha både alder og mengde.", [nameof(Trinn)]);
        }

        var utfylte = Trinn.Where(t => t.ErUtfylt).ToList();

        // Males alt etter vekt, er tabellen ikke i bruk enda, og da skal ikke
        // en tom tabell stoppe planen. Det er det ene endepunktet i en
        // forovergang.
        var tabellenErIBruk = !BlandToFor || VektdelAndel is not 100;

        if (utfylte.Count == 0 && tabellenErIBruk)
        {
            yield return new ValidationResult(
                "Legg inn minst én rad i tabellen.", [nameof(Trinn)]);
        }

        if (utfylte.Select(t => t.AlderMnd).Distinct().Count() != utfylte.Count)
        {
            // ux_forplantrinn_alder ville tatt den, men som en
            // DbUpdateException uten et felt a peke pa.
            yield return new ValidationResult(
                "To rader kan ikke ha samme alder.", [nameof(Trinn)]);
        }
    }
}
