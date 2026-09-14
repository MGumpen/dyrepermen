namespace Dyrepermen.Domain.Enums;

/// <summary>
/// Hva en henvendelse fra kontaktskjemaet gjelder.
///
/// Lagres ikke - den havner bare i emnefeltet i e-posten. Derfor ingen
/// char-konvertering slik de lagrede enumene har.
/// </summary>
public enum Kontakttype
{
    Sporsmal,
    Onske,
    Feil
}
