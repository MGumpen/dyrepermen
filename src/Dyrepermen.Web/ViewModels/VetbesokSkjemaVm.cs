using Dyrepermen.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dyrepermen.Web.ViewModels;

public sealed class VetbesokSkjemaVm
{
    public NyttVetbesokVm Ny { get; init; } = new();

    public IReadOnlyList<SelectListItem> DyrValg { get; init; } = [];

    public IReadOnlyList<SelectListItem> StedValg { get; init; } = [];

    /// <summary>Vedleggene som allerede er lagt ved, nar timen endres.</summary>
    public IReadOnlyList<Vedleggsrad> Vedlegg { get; init; } = [];

    /// <summary>False i demoen, der opplasting er stengt.</summary>
    public bool KanLasteOpp { get; init; }
}
