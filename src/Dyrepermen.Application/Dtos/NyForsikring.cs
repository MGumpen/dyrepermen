namespace Dyrepermen.Application.Dtos;

public sealed record NyForsikring(
    int? Id,
    int DyrId,
    string Selskap,
    string? PoliseNr,
    int ArspremieKr,
    int ForsikringsbelopKr,
    int EgenandelFastKr,
    int EgenandelVariabelTidels,
    DateOnly? FornyesDato);
