namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Tilstandene grensesnittet ma handtere er ulike, og de skilles her framfor
/// a la visningen gjette pa null og nuller. Se plan kapittel 8.1.
///
/// <see cref="ManglerVekt"/> er den viktige: en prosentplan uten
/// vektregistrering skal si "Registrer en vekt", ikke vise 0 gram.
/// </summary>
public sealed record ForplanResultat(
    bool HarPlan,
    bool ManglerVekt,
    int GramPerDag,
    int AntallMaltider,
    int? GrunnlagVektGram,
    DateOnly? GrunnlagDato)
{
    public static ForplanResultat IngenPlan()
        => new(false, false, 0, 0, null, null);

    public static ForplanResultat ManglerVektgrunnlag()
        => new(true, true, 0, 0, null, null);

    public static ForplanResultat Ok(
        int gram,
        int maltider,
        int? grunnlagVektGram = null,
        DateOnly? grunnlagDato = null)
        => new(true, false, gram, maltider, grunnlagVektGram, grunnlagDato);
}
