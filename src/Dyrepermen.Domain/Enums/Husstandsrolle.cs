namespace Dyrepermen.Domain.Enums;

/// <summary>
/// Lagres som char(1): B eller G.
///
/// Skillet er om personen BOR i husstanden. Bor du der, deler du ansvaret
/// og kan endre alt. Er du gjest - du passer hunden av og til - ser du alt
/// og kan logge det daglige, men ikke endre oppsettet.
///
/// Rollen ligger pa medlemskapet, ikke pa brukeren. Du kan bo hjemme hos deg
/// selv og vaere gjest hos din far. Se ADR 0009.
/// </summary>
public enum Husstandsrolle
{
    /// <summary>Bor i husstanden. Full tilgang.</summary>
    Beboer,

    /// <summary>
    /// Passer dyra av og til. Kan lese alt og logge det daglige - vekt,
    /// foring, doser, handleliste - men ikke endre dyr, medlemmer eller
    /// innstillinger.
    ///
    /// Gjesten kan SKRIVE, ikke bare lese: passer du hunden, ma du kunne
    /// notere at du ga mat og medisin. Ellers far loggen hull nettopp de
    /// dagene noen andre hadde ansvaret.
    /// </summary>
    Gjest
}
