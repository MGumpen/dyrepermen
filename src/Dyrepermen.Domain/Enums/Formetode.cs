namespace Dyrepermen.Domain.Enums;

/// <summary>
/// Lagres som char(1): P, G eller T.
///
/// <list type="bullet">
/// <item>Gram - fast mengde, star stille til den endres.</item>
/// <item>Prosent - folger siste vektregistrering.</item>
/// <item>Tabell - folger alderen, etter tabellen pa forposen. Kan i tillegg
/// blande inn et for som males etter vekt, som er det en forovergang
/// trenger.</item>
/// </list>
///
/// Se plan kapittel 8.1 og ADR 0012.
/// </summary>
public enum Formetode
{
    Prosent,
    Gram,
    Tabell
}
