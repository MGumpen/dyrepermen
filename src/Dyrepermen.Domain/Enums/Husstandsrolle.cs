namespace Dyrepermen.Domain.Enums;

/// <summary>
/// Lagres som char(1): E eller G.
///
/// Rollen ligger pa medlemskapet, ikke pa brukeren. Du kan vaere eier av din
/// egen husstand og gjest i din fars - det er hele poenget med at en bruker
/// kan vaere med i flere.
/// </summary>
public enum Husstandsrolle
{
    /// <summary>Full tilgang: kan endre dyr, medlemmer og innstillinger.</summary>
    Eier,

    /// <summary>
    /// Kan lese alt og logge det daglige - vekt, foring, doser, handleliste.
    /// Kan ikke endre dyr, medlemmer eller innstillinger.
    ///
    /// Passer du hunden, ma du kunne notere at du ga mat og medisin. Ellers
    /// far loggen hull nettopp de dagene noen andre hadde ansvaret.
    /// </summary>
    Gjest
}
