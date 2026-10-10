namespace Dyrepermen.Application.Dtos;

public enum Kurdosestatus
{
    Lagret,

    /// <summary>
    /// Kuren gar ikke i dag - den er ikke startet, er ferdig, eller er en
    /// vanlig behandling som ble regnet som gitt da den ble registrert.
    /// </summary>
    IkkeAktiv,

    /// <summary>
    /// Dagens doser er gitt. Typisk fordi noen andre i husstanden trykket
    /// forst, eller fordi en fane har statt apen.
    /// </summary>
    FullfortIdag,

    /// <summary>Behandlingen finnes ikke pa dette dyret i denne husstanden.</summary>
    FinnesIkke
}
