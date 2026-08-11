using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

public sealed record NyVeterinar(
    string Navn,
    Veterinartype Type,
    string? Telefon,
    string? Adresse,
    string? Nettside,
    string? Epost,
    string? Apningstider,
    string? Notat);
