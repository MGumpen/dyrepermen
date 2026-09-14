namespace Dyrepermen.Application.Dtos;

public sealed record NyttVetbesok(
    int DyrId,
    int? VeterinarId,
    string? Klinikk,
    DateOnly Dato,
    TimeOnly? Klokkeslett,
    string Arsak,
    string? Diagnose,
    int? KostnadKr,
    bool ForsikringKrevd,
    int? RefundertKr,
    DateOnly? NesteKontrollDato,
    string? Notat);
