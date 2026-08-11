namespace Dyrepermen.Application.Dtos;

public sealed record ForsikringRad(
    int Id,
    int DyrId,
    string DyreNavn,
    string Selskap,
    string? PoliseNr,
    int ArspremieKr,
    int ForsikringsbelopKr,
    int EgenandelFastKr,
    int EgenandelVariabelTidels,
    DateOnly? FornyesDato)
{
    /// <summary>200 tidels blir "20 %".</summary>
    public string VariabelTekst
        => $"{EgenandelVariabelTidels / 10.0:0.#} %";

    public bool ErUtlopt(DateOnly idag)
        => FornyesDato is { } d && d < idag;
}
