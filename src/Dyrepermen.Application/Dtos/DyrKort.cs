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
    SistMatet? SistMatet);
