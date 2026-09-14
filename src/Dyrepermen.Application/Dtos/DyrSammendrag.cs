namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Nok til at detaljsiden kan vise hva som ligger bak hver seksjon uten at
/// man ma klikke seg inn. En knapperad uten innhold tvinger brukeren til a
/// gjette hvor noe er.
/// </summary>
public sealed record DyrSammendrag(
    int AntallVekter,
    int? SisteVektGram,
    DateOnly? SisteVektDato,

    int AntallBehandlinger,
    string? NesteBehandlingTekst,
    DateOnly? NesteBehandlingDato,

    int AntallMedisiner,
    IReadOnlyList<string> AktiveMedisiner,

    string? ForplanTekst,
    int AntallNotater);
