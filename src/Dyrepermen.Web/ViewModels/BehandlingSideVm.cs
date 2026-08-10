using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

public sealed class BehandlingSideVm
{
    public int DyrId { get; set; }

    public string DyrNavn { get; set; } = string.Empty;

    public IReadOnlyList<BehandlingRad> Historikk { get; set; } = [];

    public NyBehandlingVm Ny { get; set; } = new();
}
