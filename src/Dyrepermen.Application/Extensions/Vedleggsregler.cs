using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Extensions;

/// <summary>
/// Hva som kan legges ved, og hvor mye. Reglene star her og ingen andre
/// steder - controlleren sjekker dem for timen lagres, og tjenesten sjekker
/// dem igjen for filen lagres. Se ADR 0018.
/// </summary>
public static class Vedleggsregler
{
    /// <summary>Plan kapittel 15: hoyst 10 MB per fil.</summary>
    public const int MaksFilByte = 10 * 1024 * 1024;

    /// <summary>
    /// Per husstand. Nedskalerte kvitteringsbilder er et par hundre KB, sa
    /// dette er flere hundre kvitteringer - men det setter et tak pa hva en
    /// husstand kan ta av den felles databasen.
    /// </summary>
    public const long MaksHusstandByte = 50L * 1024 * 1024;

    /// <summary>
    /// For alle husstandene til sammen. Neon pa gratisnivaet har 0,5 GB, og
    /// nas grensen, blokkeres ALLE skrivinger - ikke bare vedleggene. Taket
    /// holder vedleggene godt under, sa resten av appen alltid har plass.
    /// </summary>
    public const long MaksTotalByte = 300L * 1024 * 1024;

    /// <summary>
    /// Hele skjemaet med vedlegg. Et par nedskalerte bilder er under 1 MB,
    /// men en PDF pa 10 MB og et bilde som ikke ble skalert ned, skal ogsa
    /// ga gjennom. Storre foresporsler avvises av Kestrel for de leses inn.
    /// </summary>
    public const int MaksForesporselByte = 25 * 1024 * 1024;

    /// <summary>Samme lengde som kolonnen.</summary>
    public const int MaksNavnLengde = 200;

    /// <summary>
    /// MIME-typen ut fra de forste bytene, eller null nar filen ikke er en
    /// jpeg, png eller pdf. Nettleserens oppgitte type og filendelsen er
    /// begge noe klienten bestemmer, og brukes ikke.
    /// </summary>
    public static string? Innholdstype(ReadOnlySpan<byte> data)
        => data switch
        {
            [0xFF, 0xD8, 0xFF, ..] => "image/jpeg",
            [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, ..] => "image/png",
            [0x25, 0x50, 0x44, 0x46, 0x2D, ..] => "application/pdf", // %PDF-
            _ => null
        };

    /// <summary>
    /// Forste feil blant filene, som en hel setning til brukeren, eller null
    /// nar alle kan legges ved. Plassen husstanden har igjen sjekkes ikke her
    /// - den krever databasen, og ligger i tjenesten.
    /// </summary>
    public static string? Feil(IReadOnlyList<NyttVedlegg> filer)
    {
        foreach (var fil in filer)
        {
            var navn = Navn(fil.Navn);

            if (fil.Data.Length == 0)
            {
                return $"«{navn}» er tom.";
            }

            if (fil.Data.Length > MaksFilByte)
            {
                return $"«{navn}» er for stor. En fil kan være høyst {MaksFilByte / (1024 * 1024)} MB.";
            }

            if (Innholdstype(fil.Data) is null)
            {
                return $"«{navn}» er ikke et bilde eller en PDF. Bare jpg, png og pdf kan legges ved.";
            }
        }

        return null;
    }

    /// <summary>
    /// Som <see cref="Feil"/>, men for profilbildet: det ma i tillegg vaere et
    /// bilde. En PDF ville gitt et knust bilde pa dashbordet.
    /// </summary>
    public static string? ProfilbildeFeil(NyttVedlegg bilde)
    {
        if (Feil([bilde]) is { } feil)
        {
            return feil;
        }

        return Innholdstype(bilde.Data) is "image/jpeg" or "image/png"
            ? null
            : $"«{Navn(bilde.Navn)}» er ikke et bilde. Profilbildet må være jpg eller png.";
    }

    /// <summary>
    /// Filnavnet uten sti, trimmet og kuttet til kolonnens lengde. Enkelte
    /// nettlesere sender hele stien fra brukerens maskin, og den har ingenting
    /// i databasen a gjore.
    /// </summary>
    public static string Navn(string originalnavn)
    {
        var navn = originalnavn
            .Split('/', '\\')
            .Last()
            .Trim();

        if (navn.Length == 0)
        {
            return "vedlegg";
        }

        return navn.Length <= MaksNavnLengde ? navn : navn[..MaksNavnLengde];
    }
}
