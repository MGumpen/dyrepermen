using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Den lagrede planen slik den ble lagt inn. <see cref="Tabelltrinn"/> er tom
/// for alle andre metoder enn <see cref="Formetode.Tabell"/>.
/// </summary>
public sealed record ForplanRad(
    int Id,
    Formetode Metode,
    int? ProsentTidels,
    int? GramPerDag,
    int AntallMaltider,
    string? Fornavn,
    string? Notat,
    DateOnly OpprettetDato,
    DateOnly? EndretDato,
    int? VektdelAndelProsent = null,
    string? FornavnAlder = null,
    IReadOnlyList<Alderstrinn>? Tabelltrinn = null)
{
    public IReadOnlyList<Alderstrinn> Trinn => Tabelltrinn ?? [];

    /// <summary>Regelen alene, klar for <see cref="Extensions.Forberegning"/>.</summary>
    public Forplanregel TilRegel() => new(
        Metode, ProsentTidels, GramPerDag, AntallMaltider,
        VektdelAndelProsent, Trinn);
}
