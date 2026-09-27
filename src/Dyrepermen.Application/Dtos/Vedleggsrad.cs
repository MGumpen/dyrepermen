namespace Dyrepermen.Application.Dtos;

/// <summary>Et vedlegg slik det vises i en liste - uten selve filen.</summary>
public sealed record Vedleggsrad(
    int Id,
    string Navn,
    string Innholdstype,
    int StorrelseByte)
{
    /// <summary>Bilder kan vises som miniatyr. En PDF far en lenke.</summary>
    public bool ErBilde => Innholdstype.StartsWith("image/", StringComparison.Ordinal);
}
