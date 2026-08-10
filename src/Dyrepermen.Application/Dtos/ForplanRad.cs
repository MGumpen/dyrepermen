using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

public sealed record ForplanRad(
    int Id,
    Formetode Metode,
    int? ProsentTidels,
    int? GramPerDag,
    int AntallMaltider,
    string? Fornavn,
    string? Notat,
    DateOnly OpprettetDato);
