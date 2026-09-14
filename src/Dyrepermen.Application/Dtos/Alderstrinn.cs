namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Ett trinn i torrfortabellen: alder i hele maneder mot gram per dag, slik
/// det star pa forposen.
/// </summary>
public sealed record Alderstrinn(int AlderMnd, int GramPerDag);
