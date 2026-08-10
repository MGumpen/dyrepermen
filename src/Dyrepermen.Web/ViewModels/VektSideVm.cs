using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

public sealed class VektSideVm
{
    public int DyrId { get; set; }

    public string DyrNavn { get; set; } = string.Empty;

    public IReadOnlyList<VektRad> Historikk { get; set; } = [];

    public NyVektVm Ny { get; set; } = new();
}
