using System.Globalization;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Extensions;

/// <summary>
/// Innholdet i e-posten fra kontaktskjemaet. Ren tekst uten databasetilgang,
/// sa den kan enhetstestes uten a sende noe.
/// </summary>
public static class Kontaktepost
{
    /// <summary>
    /// Brukes av valideringen og av maxlength i skjemaet. Star tallet to
    /// steder, lover skjemaet noe annet enn serveren godtar.
    /// </summary>
    public const int MaksLengde = 2000;

    // Eksplisitt kultur, ikke tradens. Samme monster som Tidssone.
    private static readonly CultureInfo Norsk = new("nb-NO");

    /// <summary>Teksten brukeren ser i nedtrekkslisten, og som star i emnet.</summary>
    public static string TypeTekst(Kontakttype type) => type switch
    {
        Kontakttype.Sporsmal => "Spørsmål",
        Kontakttype.Onske => "Ønske om endring",
        Kontakttype.Feil => "Feil",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    /// <summary>
    /// Kun typen, aldri noe brukeren har skrevet. Et emne bygget av fritekst
    /// er et emne noen kan sprøyte linjeskift og ekstra hoder inn i.
    /// </summary>
    public static string Emne(Kontakttype type) => $"Dyrepermen: {TypeTekst(type)}";

    public static string Tekst(
        Kontakttype type,
        string melding,
        int brukerId,
        string visningsnavn,
        string epost,
        DateTimeOffset sendt)
    {
        var tid = Tidssone.TilLokal(sendt).ToString("dd.MM.yyyy 'kl.' HH:mm", Norsk);

        return $"""
            Ny henvendelse fra kontaktskjemaet.

            Type: {TypeTekst(type)}
            Fra: {visningsnavn} (bruker-ID {brukerId})
            E-post: {epost}
            Sendt: {tid}

            Svar på denne e-posten, så går svaret rett til avsenderen.

            ---

            {melding}
            """;
    }
}
