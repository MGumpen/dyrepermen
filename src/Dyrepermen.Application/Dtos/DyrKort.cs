using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Ett kort per aktivt dyr pa dashbordet.
///
/// SisteVektGram er nullbar med vilje: uten registrering skal grensesnittet
/// vise "Ingen vekt registrert", ikke "0 kg". Se plan kapittel 10.3.
/// </summary>
public sealed record DyrKort(
    int Id,
    string Navn,
    Art Art,
    string? BildeFilnavn,
    DateOnly? Fodselsdato,
    bool ForingsloggAktiv,
    int? SisteVektGram,
    DateOnly? SisteVektDato);
