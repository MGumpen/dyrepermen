namespace Dyrepermen.Web.ViewModels;

public sealed class MinKontoVm
{
    public string Visningsnavn { get; set; } = string.Empty;

    public SlettKontoVm Slett { get; set; } = new();
}
