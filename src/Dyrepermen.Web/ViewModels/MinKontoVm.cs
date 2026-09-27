namespace Dyrepermen.Web.ViewModels;

public sealed class MinKontoVm
{
    public string Visningsnavn { get; set; } = string.Empty;

    public TelefonVm Telefon { get; set; } = new();

    /// <summary>
    /// Skjemaet for telefonnummeret vises. Ellers star nummeret som en vanlig
    /// rad i profilen, som navn og e-post, med en lenke for a endre det.
    /// </summary>
    public bool EndrerTelefon { get; set; }

    public SlettKontoVm Slett { get; set; } = new();
}
