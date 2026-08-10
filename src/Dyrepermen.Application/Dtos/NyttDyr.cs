using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Inndata ved opprettelse. Funksjonsbryterne er ikke med - de arves fra
/// husstandens standardverdier. Se plan kapittel 8.2.
/// </summary>
public sealed record NyttDyr(
    string Navn,
    Art Art,
    Kjonn Kjonn,
    string? Rase,
    DateOnly? Fodselsdato,
    string? ChipNr,
    string? RegNrNkk,
    bool Kastrert);
