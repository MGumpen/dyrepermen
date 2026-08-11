using System.ComponentModel.DataAnnotations;
using Dyrepermen.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dyrepermen.Web.ViewModels;

public sealed class ForsikringVm
{
    public IReadOnlyList<ForsikringRad> Poliser { get; set; } = [];

    public IReadOnlyList<SelectListItem> DyrValg { get; set; } = [];

    public NyForsikringVm Ny { get; set; } = new();
}

public sealed class NyForsikringVm
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Velg hvilket dyr polisen gjelder.")]
    [Display(Name = "Gjelder")]
    public int DyrId { get; set; }

    [Required(ErrorMessage = "Skriv inn selskapet.")]
    [StringLength(80, ErrorMessage = "Selskapet kan være høyst 80 tegn.")]
    [Display(Name = "Selskap")]
    public string Selskap { get; set; } = string.Empty;

    [StringLength(40, ErrorMessage = "Polisenummeret kan være høyst 40 tegn.")]
    [Display(Name = "Polisenummer")]
    public string? PoliseNr { get; set; }

    [Range(0, 1000000, ErrorMessage = "Premien må være mellom 0 og 1 000 000.")]
    [Display(Name = "Årspremie i kroner")]
    public int ArspremieKr { get; set; }

    [Range(0, 100000000, ErrorMessage = "Beløpet er utenfor gyldig område.")]
    [Display(Name = "Forsikringsbeløp i kroner")]
    public int ForsikringsbelopKr { get; set; }

    [Range(0, 1000000, ErrorMessage = "Egenandelen må være mellom 0 og 1 000 000.")]
    [Display(Name = "Fast egenandel i kroner")]
    public int EgenandelFastKr { get; set; }

    /// <summary>
    /// Brukeren skriver prosent, for eksempel 20. Lagres som tidels
    /// prosent, altsa 200 - samme monster som forplanens prosentsats.
    /// </summary>
    [Range(0, 100, ErrorMessage = "Prosenten må være mellom 0 og 100.")]
    [Display(Name = "Variabel egenandel i prosent")]
    public decimal EgenandelVariabelProsent { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fornyes")]
    public DateOnly? FornyesDato { get; set; }
}
