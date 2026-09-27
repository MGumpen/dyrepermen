namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Svaret nar filer lastes opp - vedlegg til et besok eller et profilbilde.
/// <see cref="Feil"/> er en hel setning som kan vises til brukeren slik den er.
/// </summary>
public sealed record Vedleggsresultat(bool Ok, bool FinnesIkke, string? Feil)
{
    public static Vedleggsresultat Lagret() => new(true, false, null);

    /// <summary>Besoket eller dyret finnes ikke i denne husstanden.</summary>
    public static Vedleggsresultat Mangler() => new(false, true, null);

    public static Vedleggsresultat Avvist(string feil) => new(false, false, feil);
}
