using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Innholdet i en forplan slik brukeren har fylt det ut. Samme type brukes
/// bade nar en plan opprettes og nar den redigeres - feltene er de samme, og
/// to nesten like typer ville drevet fra hverandre.
///
/// De tre metodene er gjensidig utelukkende. Tjenesten nuller ut feltene som
/// ikke hoerer til valgt metode, slik at ck_forplan_verdi aldri brytes.
/// </summary>
public sealed record Forplaninnhold(
    int DyrId,
    Formetode Metode,
    int? ProsentTidels,
    int? GramPerDag,
    int AntallMaltider,
    string? Fornavn,
    string? Notat,
    int? VektdelAndelProsent = null,
    string? FornavnAlder = null,
    IReadOnlyList<Alderstrinn>? Tabelltrinn = null);
