namespace Dyrepermen.Application.Dtos;

/// <summary>Ett punkt, med bade verdien og plasseringen i SVG-koordinater.</summary>
public sealed record Grafpunkt(
    DateOnly Dato,
    int VektGram,
    double X,
    double Y);

/// <summary>En vannrett hjelpelinje med etiketten sin.</summary>
public sealed record Aksemerke(double Y, int Gram)
{
    /// <summary>
    /// Runde tall pa aksen: "12", ikke "12,00". To desimaler pa hvert
    /// aksemerke er stoy - presisjonen hoerer hjemme i tabellen og i boblen.
    /// </summary>
    public string Etikett => Gram % 1000 == 0
        ? (Gram / 1000).ToString(System.Globalization.CultureInfo.GetCultureInfo("nb-NO"))
        : (Gram / 1000.0).ToString("0.#",
            System.Globalization.CultureInfo.GetCultureInfo("nb-NO"));
}

/// <summary>
/// Ferdig utregnet geometri for vektgrafen. Utregningen ligger i
/// Application fordi den er ren logikk - da kan skaleringen enhetstestes
/// uten a rendre noe.
/// </summary>
public sealed record Vektgrafdata(
    IReadOnlyList<Grafpunkt> Punkter,
    IReadOnlyList<Aksemerke> Merker,
    string Linje,
    string Flate,
    double Bredde,
    double Hoyde);
