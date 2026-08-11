using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dyrepermen.Web.ViewModels;

public sealed class VetbesokSkjemaVm
{
    public NyttVetbesokVm Ny { get; init; } = new();

    public IReadOnlyList<SelectListItem> DyrValg { get; init; } = [];

    public IReadOnlyList<SelectListItem> StedValg { get; init; } = [];
}
