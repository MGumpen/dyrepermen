using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Ett kort per aktivt dyr pa dashbordet.
///
/// SisteVektGram er nullbar med vilje: uten registrering skal grensesnittet
/// vise "Ingen vekt registrert", ikke "0 kg". Se plan kapittel 10.3.
///
/// ForplanTekst og NesteBehandling er med fordi dashbordet skal svare pa
/// "hva gjenstar i dag" uten at man ma klikke seg inn pa hvert dyr.
/// </summary>
public sealed record DyrKort(
    int Id,
    string Navn,
    Art Art,
    string? BildeFilnavn,
    DateOnly? Fodselsdato,
    bool ForingsloggAktiv,
    int? SisteVektGram,
    DateOnly? SisteVektDato,
    string? ForplanTekst,
    string? NesteBehandlingTekst,
    DateOnly? NesteBehandlingDato,
    IReadOnlyList<string> AktiveMedisiner,

    /// <summary>Kun nar foringsloggen er pa for dyret.</summary>
    SistMatet? SistMatet,

    /// <summary>
    /// Gram til ETT maltid, ikke til hele dagen. Null nar dyret ikke har
    /// aktiv forplan, eller nar planen regner prosent av vekt og vekten
    /// mangler - da finnes det ikke noe tall a vise.
    /// </summary>
    int? PorsjonGram,

    /// <summary>Maltider planen sier per dag.</summary>
    int AntallMaltider,

    /// <summary>
    /// Foringer registrert siden midnatt NORSK tid. Sammen med
    /// AntallMaltider gir det "maltid 2 av 3" pa dashbordet.
    /// </summary>
    int MaltiderIDag)
{
    /// <summary>
    /// Nummeret pa maltidet som star for tur. Er alle gitt, peker det forbi
    /// planen - og visningen sier "alle gitt" i stedet.
    /// </summary>
    public int NesteMaltid => MaltiderIDag + 1;

    public bool AlleMaltiderGitt =>
        AntallMaltider > 0 && MaltiderIDag >= AntallMaltider;
}
