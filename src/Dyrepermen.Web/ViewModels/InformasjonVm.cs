using System.ComponentModel.DataAnnotations;
using Dyrepermen.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dyrepermen.Web.ViewModels;

public sealed class InformasjonVm
{
    public IReadOnlyList<DyreOversikt> Dyr { get; set; } = [];

    public IReadOnlyList<InformasjonRad> FellesNotater { get; set; } = [];

    public IReadOnlyList<SelectListItem> DyrValg { get; set; } = [];

    public NyttNotatVm Ny { get; set; } = new();
}

public sealed class NyttNotatVm
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Gi notatet en tittel.")]
    [StringLength(80, ErrorMessage = "Tittelen kan være høyst 80 tegn.")]
    [Display(Name = "Tittel")]
    public string Tittel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Skriv inn teksten.")]
    [StringLength(2000, ErrorMessage = "Notatet kan være høyst 2000 tegn.")]
    [Display(Name = "Notat")]
    public string Tekst { get; set; } = string.Empty;

    [Display(Name = "Gjelder")]
    public int? DyrId { get; set; }
}
