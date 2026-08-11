using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

public sealed record Husstandsmedlem(
    int BrukerId,
    string Visningsnavn,
    string? Epost,
    bool ErDegSelv,
    Husstandsrolle Rolle);
