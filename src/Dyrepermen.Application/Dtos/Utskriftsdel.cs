namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Delene av permen som kan velges til utskriften. Flagg, slik at et utvalg
/// er ett tall og ikke en liste - tjenesten spor bare om et flagg er satt.
///
/// Rekkefolgen her er rekkefolgen pa arket og i valgskjemaet.
/// </summary>
[Flags]
public enum Utskriftsdel
{
    Ingen = 0,
    OmDyret = 1,
    Forplan = 2,
    Vekt = 4,
    Behandlinger = 8,
    Medisiner = 16,
    Forsikring = 32,
    Notater = 64,

    /// <summary>Notatene som ikke horer til et bestemt dyr.</summary>
    FellesNotater = 128,

    Alle = OmDyret | Forplan | Vekt | Behandlinger | Medisiner | Forsikring
         | Notater | FellesNotater
}
