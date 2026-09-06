using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

/// <summary>
/// De to metodene er gjensidig utelukkende. Tjenesten nuller ut feltet som
/// ikke hoerer til valgt metode, slik at ck_forplan_verdi aldri brytes.
/// </summary>
public sealed record NyForplan(
    int DyrId,
    Formetode Metode,
    int? ProsentTidels,
    int? GramPerDag,
    int AntallMaltider,
    string? Fornavn,
    string? Notat);
