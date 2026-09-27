namespace Dyrepermen.Application.Dtos;

/// <summary>
/// En fil slik den kom inn fra skjemaet. Innholdstypen er ikke med: den
/// avgjores av innholdet selv, ikke av det nettleseren oppgir.
/// </summary>
public sealed record NyttVedlegg(string Navn, byte[] Data);
