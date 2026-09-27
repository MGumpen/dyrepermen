namespace Dyrepermen.Application.Dtos;

public enum Gittstatus
{
    Lagret,

    /// <summary>
    /// En nyere behandling av samme slag finnes allerede - typisk fordi
    /// noen andre i husstanden krysset av forst, eller fordi det samme
    /// skjemaet ble sendt to ganger.
    /// </summary>
    AlleredeFulgtOpp,

    /// <summary>
    /// Behandlingen ble gitt i dag, eller har en dato fram i tid. Se
    /// Behandlingsintervall.KanGisIgjen.
    /// </summary>
    GittIdag,

    /// <summary>Behandlingen finnes ikke pa dette dyret i denne husstanden.</summary>
    FinnesIkke
}
