using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

public sealed record HandlelisteRad(
    int Id,
    string Tekst,
    int Antall,
    HandlelisteStatus Status,
    /// <summary>Null betyr at punktet ikke er knyttet til et dyr.</summary>
    string? DyreNavn,
    string? OpprettetAv,
    DateOnly OpprettetDato);
