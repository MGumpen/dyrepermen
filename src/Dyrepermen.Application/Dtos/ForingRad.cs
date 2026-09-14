namespace Dyrepermen.Application.Dtos;

public sealed record ForingRad(
    int Id,
    DateTimeOffset Tidspunkt,
    int? MengdeGram,
    string? GittAv,
    string? Kommentar);
