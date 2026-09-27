using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

public sealed record BehandlingRad(
    int Id,
    BehandlingType Type,
    string? Preparat,
    DateOnly Dato,
    DateOnly? NesteDato,
    string? Notat,

    /// <summary>
    /// Neste gang er ikke fulgt opp enna: ingen nyere behandling av samme
    /// type og preparat finnes pa dyret. Bare da kan raden krysses av som
    /// gitt. Se ADR 0016.
    /// </summary>
    bool ErApen = false);
