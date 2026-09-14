namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Forventede feil er ikke unntak. De returneres som resultattype og vises i
/// ModelState. Unntak reserveres for det som ikke skal kunne skje.
/// Se plan kapittel 9.2.
/// </summary>
public enum DyrFeil
{
    Ingen,
    FinnesIkke,
    ChipFinnes,
    RegnrFinnes,
    Samtidighet
}
