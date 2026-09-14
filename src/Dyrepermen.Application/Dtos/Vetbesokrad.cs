namespace Dyrepermen.Application.Dtos;

/// <summary>En time - kommende eller gjennomfort.</summary>
public sealed record Vetbesokrad(
    int Id,
    int DyrId,
    string DyreNavn,
    int? VeterinarId,
    string? VeterinarNavn,
    string? Klinikk,
    DateOnly Dato,
    TimeOnly? Klokkeslett,
    string Arsak,
    string? Diagnose,
    int? KostnadKr,
    bool ForsikringKrevd,
    int? RefundertKr,
    DateOnly? NesteKontrollDato,
    string? Notat)
{
    /// <summary>
    /// Navnet fra listen nar besoket peker dit, ellers friteksten. Ett sted
    /// a sporre, sa visningen slipper a velge mellom to felter.
    /// </summary>
    public string? Sted => VeterinarNavn ?? Klinikk;

    /// <summary>
    /// Ingen statuskolonne - en time er kommende sa lenge datoen ikke har
    /// passert. En status matte hukes av manuelt, og den som glemte det ville
    /// hatt en "kommende" time fra i fjor staende.
    /// </summary>
    public bool ErKommende(DateOnly idag) => Dato >= idag;

    /// <summary>Hva besoket kostet etter at forsikringen har gjort sitt.</summary>
    public int? NettoKr => KostnadKr is null
        ? null
        : KostnadKr.Value - (RefundertKr ?? 0);
}
