namespace Dyrepermen.Application.Dtos;

public sealed record Dashbord(
    IReadOnlyList<DyrKort> Dyr,
    IReadOnlyList<Paminnelse> Forfaller,
    IReadOnlyList<HandlelisteRad> Handleliste,

    /// <summary>
    /// Husstandsbryter, ikke per dyr. Styrer om godbitknappen tegnes -
    /// tjenesten stenger endepunktet uavhengig av denne.
    /// </summary>
    bool GodbitloggAktiv,

    /// <summary>
    /// Forsikringene som gjelder na. Utlopte er utelatt - de dukker opp i
    /// Forfaller med merkelappen "forfalt", og hoerer ikke hjemme i en liste
    /// som skal svare pa "hva er dyrene dekket av".
    /// </summary>
    IReadOnlyList<ForsikringRad> Forsikringer,

    /// <summary>
    /// Stedene med telefonnummer, fastveterinaeren oeverst og vakta rett
    /// under. Kun de som faktisk kan ringes.
    ///
    /// Samme <see cref="Veterinarrad"/> som veterinaersiden bruker, slik at
    /// tel:-lenken bygges ett sted og ikke kan sprike mellom de to sidene.
    /// </summary>
    IReadOnlyList<Veterinarrad> Veterinarer);
