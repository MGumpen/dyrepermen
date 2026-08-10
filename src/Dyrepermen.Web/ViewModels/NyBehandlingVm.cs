using System.ComponentModel.DataAnnotations;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Web.ViewModels;

public sealed class NyBehandlingVm
{
    [Display(Name = "Type")]
    public BehandlingType Type { get; set; } = BehandlingType.Vaksine;

    [StringLength(80, ErrorMessage = "Preparatet kan være høyst 80 tegn.")]
    [Display(Name = "Preparat")]
    public string? Preparat { get; set; }

    [Required(ErrorMessage = "Velg en dato.")]
    [DataType(DataType.Date)]
    [Display(Name = "Dato")]
    public DateOnly Dato { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Driver paminnelsene pa dashbordet. Valgfri.</summary>
    [DataType(DataType.Date)]
    [Display(Name = "Neste gang")]
    public DateOnly? NesteDato { get; set; }

    [StringLength(500, ErrorMessage = "Notatet kan være høyst 500 tegn.")]
    [Display(Name = "Notat")]
    public string? Notat { get; set; }
}
