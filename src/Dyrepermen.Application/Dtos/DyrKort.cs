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
    int MaltiderIDag,

    /// <summary>Foret planen sier, som forhandsvalg i dialogen.</summary>
    string? Fornavn,

    /// <summary>
    /// Godbiter siden midnatt. Talt for seg - en godbit er ikke et maltid,
    /// og skal aldri fa telleren til a si at middagen er gitt.
    /// </summary>
    int GodbiterIDag)
{
    /// <summary>
    /// Nummeret pa maltidet som star for tur. Er alle gitt, peker det forbi
    /// planen - og visningen sier "alle gitt" i stedet.
    /// </summary>
    public int NesteMaltid => MaltiderIDag + 1;

    public bool AlleMaltiderGitt =>
        AntallMaltider > 0 && MaltiderIDag >= AntallMaltider;

    /// <summary>
    /// Om en teller av typen "nr. 2 av 3" i det hele tatt betyr noe.
    ///
    /// Uten foringslogg registreres ingen maltider, og MaltiderIDag settes
    /// til 0 av tjenesten. Telleren ville da statt pa "nr. 1 av 3" fra
    /// morgen til kveld - et tall som ser ut som framdrift, men som aldri
    /// beveger seg uansett hvor mange ganger dyret faktisk far mat.
    ///
    /// Er den av, skal kortet vise hva planen SIER - porsjon og antall
    /// maltider - i stedet for a late som om den folger med.
    /// </summary>
    public bool TellerMaltider => ForingsloggAktiv && AntallMaltider > 0;
}
