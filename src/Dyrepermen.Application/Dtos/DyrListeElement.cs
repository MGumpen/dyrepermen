using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

public sealed record DyrListeElement(
    int Id,
    string Navn,
    Art Art,
    string? Rase,
    DateOnly? Fodselsdato);
