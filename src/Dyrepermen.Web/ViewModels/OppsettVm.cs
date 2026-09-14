using System.ComponentModel.DataAnnotations;

namespace Dyrepermen.Web.ViewModels;

public sealed class OppsettVm
{
    [Required(ErrorMessage = "Gi husstanden et navn.")]
    [StringLength(80, ErrorMessage = "Navnet kan være høyst 80 tegn.")]
    [Display(Name = "Navn på husstanden")]
    public string Navn { get; set; } = string.Empty;
}
