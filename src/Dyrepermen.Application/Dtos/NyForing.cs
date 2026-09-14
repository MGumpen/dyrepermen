using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Tidspunkt er BEVISST ikke med. Tjenesten setter DateTimeOffset.UtcNow
/// selv, slik at klienten ikke kan pavirke det. Se plan kapittel 8.2.
/// </summary>
public sealed record NyForing(
    int DyrId,
    int? MengdeGram,
    string? Kommentar,
    int? GittAvBrukerId,
    Foringstype Type = Foringstype.Maltid,

    /// <summary>Hva som ble gitt. Fritekst, med forslag fra tidligere rader.</summary>
    string? Fornavn = null);
