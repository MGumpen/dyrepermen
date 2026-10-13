namespace Dyrepermen.Application.Dtos;

/// <summary>
/// En dose i en kur, slik historikken viser den. <see cref="GittAv"/> er
/// null nar brukeren er slettet - visningen skriver "slettet bruker".
/// </summary>
public sealed record Kurdose(DateTimeOffset GittTid, string? GittAv);
