namespace Dyrepermen.Application.Dtos;

public sealed record VektRad(
    int Id,
    int VektGram,
    DateOnly Dato,
    string? RegistrertAv);
