namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Hvor en paminnelse kommer fra. Styrer hvilken handling dashbordet tilbyr
/// pa raden: gitt, gi dose, gjennomfort eller bestill time.
/// </summary>
public enum Kilde
{
    Behandling,
    Medisin,
    Forsikring,
    Vetbesok,

    /// <summary>
    /// Avtalt oppfolging fra et besok. Egen kilde fordi den folges opp med
    /// en ny time, ikke ved a rette timen den kom fra.
    /// </summary>
    Vetkontroll,

    /// <summary>
    /// En behandling som gar over flere dager eller gis flere ganger per
    /// dag, og som gar i dag. Folges opp med en dose om gangen, ikke med
    /// dialogen for neste gang. KildeId er behandlingen. Se ADR 0019.
    /// </summary>
    Kur
}
