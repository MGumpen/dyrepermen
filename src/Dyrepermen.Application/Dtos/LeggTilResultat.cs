namespace Dyrepermen.Application.Dtos;

public enum LeggTilResultat
{
    LagtTil,
    VenterPaRegistrering,
    AlleredeMedlem,

    /// <summary>
    /// Adressen tilhorer allerede en annen husstand. Meldingen til brukeren
    /// ma vaere noytral: a svare "personen tilhorer allerede en husstand"
    /// bekrefter for en fremmed at adressen er registrert i systemet.
    /// </summary>
    TilhorerAnnenHusstand
}
