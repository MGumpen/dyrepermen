namespace Dyrepermen.Domain.Enums;

/// <summary>
/// Hva slags sted dette er. Typen bestemmer rekkefolgen i listen: nar noe har
/// skjedd og du apner siden, er det ikke fastveterinaeren du leter etter.
/// </summary>
public enum Veterinartype
{
    /// <summary>Den du bruker til vanlig.</summary>
    Fast = 0,

    /// <summary>Vakt utenom apningstid.</summary>
    Vakt = 1,

    /// <summary>Dyresykehus.</summary>
    Sykehus = 2,

    /// <summary>Spesialist, klinikk pa reise, eller noe annet.</summary>
    Annet = 3
}
