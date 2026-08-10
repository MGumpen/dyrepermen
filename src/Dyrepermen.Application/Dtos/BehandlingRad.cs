using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

public sealed record BehandlingRad(
    int Id,
    BehandlingType Type,
    string? Preparat,
    DateOnly Dato,
    DateOnly? NesteDato,
    string? Notat);
