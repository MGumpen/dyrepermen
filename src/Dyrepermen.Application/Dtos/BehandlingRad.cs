using Dyrepermen.Application.Extensions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

public sealed record BehandlingRad(
    int Id,
    BehandlingType Type,
    string? Preparat,
    string? Styrke,
    DateOnly Dato,
    DateOnly? NesteDato,
    string? Notat,
    int AntallDager,
    int GangerPerDag,

    /// <summary>
    /// Neste gang er ikke fulgt opp enna: ingen nyere behandling av samme
    /// type og preparat finnes pa dyret. Bare da kan raden krysses av som
    /// gitt. Se ADR 0016.
    /// </summary>
    bool ErApen)
{
    /// <summary>
    /// Knappen "gitt i dag" vises bare her: paminnelsen venter, og
    /// behandlingen ble gitt for i dag. Se Behandlingsintervall.KanGisIgjen.
    /// </summary>
    public bool KanKrysseAv(DateOnly idag)
        => ErApen && Behandlingsintervall.KanGisIgjen(Dato, idag);

    /// <summary>
    /// Dosene i en kur, eldste forst, med hvem som ga dem. Tom for en vanlig
    /// behandling. Ikke i konstruktoren: utskriften trenger dem ikke, og
    /// historikken fyller dem i samme sporring. Se ADR 0019.
    /// </summary>
    public IReadOnlyList<Kurdose> Doser { get; init; } = [];

    /// <summary>"1 gang daglig i 3 dager", eller null for en vanlig behandling.</summary>
    public string? Kurtekst => Behandlingskur.Beskrivelse(AntallDager, GangerPerDag);
}
