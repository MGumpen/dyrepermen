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

    /// <summary>
    /// Alle som venter pa neste gang, forste forfall forst. Ikke bare den
    /// neste: en hund med ormekur, flattmiddel og vaksine har tre datoer a
    /// holde styr pa, og siden skal vise alle tre.
    /// </summary>
    IReadOnlyList<KommendeBehandling> KommendeBehandlinger,

    int AntallMedisiner,
    IReadOnlyList<string> AktiveMedisiner,

    string? ForplanTekst,
    int AntallNotater);
