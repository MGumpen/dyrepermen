namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Dobbeltdoseringssjekken er hele poenget med at to personer deler samme
/// husstand: den ene skal ikke kunne gi en dose den andre nettopp ga.
///
/// <see cref="KreverBekreftelse"/> blokkerer ikke permanent. Veterinaeren kan
/// ha bedt om en ekstra dose, og da skal appen kunne overstyres - men bevisst,
/// ikke ved et uhell.
/// </summary>
public sealed record DoseResultat(
    bool Ok,
    bool KreverBekreftelse,
    string? Melding)
{
    public static DoseResultat Lagret() => new(true, false, null);

    public static DoseResultat FinnesIkke() => new(false, false, null);

    public static DoseResultat ForTidlig(string melding)
        => new(false, true, melding);
}
