using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

public sealed class MedisinSideVm
{
    public int DyrId { get; set; }

    public string DyrNavn { get; set; } = string.Empty;

    public IReadOnlyList<MedisinRad> Medisiner { get; set; } = [];

    public NyMedisinVm Ny { get; set; } = new();
}
