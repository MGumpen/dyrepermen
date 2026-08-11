namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Tidspunkt er BEVISST ikke med. Tjenesten setter DateTimeOffset.UtcNow
/// selv, slik at klienten ikke kan pavirke det. Se plan kapittel 8.2.
/// </summary>
public sealed record NyForing(
    int DyrId,
    int? MengdeGram,
    string? Kommentar,
    int? GittAvBrukerId);
