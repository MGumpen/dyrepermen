using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

public sealed record NyBehandling(
    int DyrId,
    BehandlingType Type,
    string? Preparat,
    DateOnly Dato,
    DateOnly? NesteDato,
    string? Notat);
