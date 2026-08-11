namespace Dyrepermen.Application.Dtos;

public sealed record Dashbord(
    IReadOnlyList<DyrKort> Dyr,
    IReadOnlyList<Paminnelse> Forfaller,
    IReadOnlyList<HandlelisteRad> Handleliste,

    /// <summary>
    /// Husstandsbryter, ikke per dyr. Styrer om godbitknappen tegnes -
    /// tjenesten stenger endepunktet uavhengig av denne.
    /// </summary>
    bool GodbitloggAktiv);
