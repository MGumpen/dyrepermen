namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Det dialogen for "gitt i dag" trenger: hva som krysses av, og hva som
/// sto sist. Appen foreslar ingen neste gang - feltet star tomt, og
/// brukeren fyller det inn selv. Se ADR 0016.
/// </summary>
public sealed record Gittgrunnlag(
    int DyrId,
    int BehandlingId,
    string DyreNavn,
    string Beskrivelse,
    DateOnly ForrigeDato,
    DateOnly? ForrigeNeste,

    /// <summary>
    /// Apen paminnelse, gitt for i dag. Er den false, tegnes dialogen med en
    /// forklaring og uten knapp.
    /// </summary>
    bool KanGis);
