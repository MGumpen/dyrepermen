namespace Dyrepermen.Domain.Enums;

/// <summary>
/// Lagres som char(1): P eller G.
/// Prosentmetoden er levende - den leser siste vektregistrering hver gang.
/// Fast mengde star stille til den endres. Se plan kapittel 8.1.
/// </summary>
public enum Formetode
{
    Prosent,
    Gram
}
