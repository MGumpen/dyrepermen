namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Det regelen skal regnes mot: siste vekt, fodselsdato og dagens dato.
///
/// Datoen sendes inn framfor a leses av <c>DateTime.UtcNow</c> inne i
/// beregningen. Torrformengden folger alderen, og en regel som henter
/// klokka selv lar seg ikke teste for en valp som er 17 uker gammel.
/// </summary>
public sealed record Beregningsgrunnlag(
    int? SisteVektGram,
    DateOnly? SisteVektDato,
    DateOnly? Fodselsdato,
    DateOnly Idag);
