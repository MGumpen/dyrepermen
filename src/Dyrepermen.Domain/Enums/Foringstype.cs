namespace Dyrepermen.Domain.Enums;

/// <summary>
/// Skiller maltider fra godbiter i foringsloggen.
///
/// Uten skillet ville en godbit talt som et maltid, og dashbordet sagt
/// "maltid 3 av 3" fordi noen ga hunden en ostebit. Da slutter telleren a
/// bety noe, og den som kommer hjem vet ikke om middagen er gitt.
/// </summary>
public enum Foringstype
{
    /// <summary>CLR-standard med vilje: en rad uten uttrykt type er et maltid.</summary>
    Maltid = 0,

    Godbit = 1
}
