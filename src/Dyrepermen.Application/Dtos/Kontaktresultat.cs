namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Utfallet av en henvendelse. IkkeSattOpp og Feilet er skilt fordi brukeren
/// skal fa vite om det lonner seg a prove igjen om litt.
/// </summary>
public enum Kontaktresultat
{
    Sendt,
    IkkeSattOpp,
    Feilet
}
