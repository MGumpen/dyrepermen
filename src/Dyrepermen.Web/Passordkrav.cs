namespace Dyrepermen.Web;

/// <summary>
/// Passordreglene, ett sted.
///
/// De sto tidligere tre steder: Identity i Program.cs, DataAnnotations i
/// RegistrerVm, og en hjelpetekst i visningen. De sprikte - skjemaet lovet
/// "minst 10 tegn" mens Identity i tillegg krevde tall, sma og store
/// bokstaver og spesialtegn, fordi standardverdiene aldri ble overstyrt.
///
/// Resultatet var at et passord kunne passere valideringen i nettleseren og
/// bli avvist av serveren med et krav brukeren aldri hadde sett. Det er ikke
/// et vanskelig krav a oppfylle - det er et usynlig et.
///
/// Alle tre leser na herfra, og PassordkravTester feiler hvis Identity og
/// denne filen kommer i utakt igjen.
/// </summary>
public static class Passordkrav
{
    public const int MinLengde = 6;

    /// <summary>
    /// Eneste sammensetningskrav. Appen er for privat bruk i en husstand;
    /// krav som gjor at folk ikke gidder a lage konto, beskytter ingenting.
    /// </summary>
    public const bool KreverStorBokstav = true;

    /// <summary>Vises under passordfeltet. Ordrett de reglene som gjelder.</summary>
    public const string Hjelpetekst =
        "Minst 6 tegn, og minst én stor bokstav.";

    public const string ForKort =
        "Passordet må være minst 6 tegn.";

    public const string ManglerStorBokstav =
        "Passordet må inneholde minst én stor bokstav.";
}
