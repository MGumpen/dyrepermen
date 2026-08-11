using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

/// <summary>Ett sted i listen over veterinaerer.</summary>
public sealed record Veterinarrad(
    int Id,
    string Navn,
    Veterinartype Type,
    string? Telefon,
    string? Adresse,
    string? Nettside,
    string? Epost,
    string? Apningstider,
    string? Notat,

    /// <summary>Hvor mange besok som peker hit. Vises som kontekst i listen.</summary>
    int AntallBesok)
{
    /// <summary>
    /// Nummeret som tel:-lenke. Mellomrom og bindestrek ma bort, ellers
    /// avviser noen telefoner lenken - men det som VISES beholder
    /// formateringen brukeren skrev.
    /// </summary>
    public string? TelefonLenke => Telefon is null
        ? null
        : new string(Telefon.Where(c => char.IsDigit(c) || c == '+').ToArray());
}
