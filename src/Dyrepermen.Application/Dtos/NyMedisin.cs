namespace Dyrepermen.Application.Dtos;

public sealed record NyMedisin(
    int DyrId,
    string Navn,
    string Dose,
    int IntervallTimer,
    DateOnly StartDato,
    DateOnly? SluttDato);
