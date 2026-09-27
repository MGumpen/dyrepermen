namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Svaret nar filer legges ved et besok. <see cref="Feil"/> er en hel
/// setning som kan vises til brukeren slik den er.
/// </summary>
public sealed record Vedleggsresultat(bool Ok, bool FinnesIkke, string? Feil)
{
    public static Vedleggsresultat Lagret() => new(true, false, null);

    public static Vedleggsresultat BesoketFinnesIkke() => new(false, true, null);

    public static Vedleggsresultat Avvist(string feil) => new(false, false, feil);
}
