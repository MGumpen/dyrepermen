using Dyrepermen.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dyrepermen.Web.ViewModels;

public sealed class HandlelisteVm
{
    public IReadOnlyList<HandlelisteRad> Punkter { get; set; } = [];

    public IReadOnlyList<SelectListItem> Dyr { get; set; } = [];

    public bool HarKjopte => Punkter.Any(
        p => p.Status == Domain.Enums.HandlelisteStatus.Kjopt);
}
