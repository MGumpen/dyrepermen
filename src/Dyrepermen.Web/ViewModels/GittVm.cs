using System.ComponentModel.DataAnnotations;
using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Web.ViewModels;

/// <summary>Det som postes fra dialogen for "gitt i dag".</summary>
public sealed class GittVm : IValidatableObject
{
    /// <summary>Tomt betyr ingen ny paminnelse.</summary>
    [DataType(DataType.Date)]
    [Display(Name = "Neste gang")]
    public DateOnly? NesteDato { get; set; }

    /// <summary>Send brukeren tilbake til dashbordet etterpa.</summary>
    public bool FraOversikt { get; set; }

    /// <summary>
    /// Behandlingen gis i dag, sa neste gang ma vaere etter i dag. En dato i
    /// dag eller tidligere ville gitt en paminnelse som var forfalt i det
    /// oyeblikket den ble lagret.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (NesteDato is { } neste && neste <= Tidssone.Idag(DateTimeOffset.UtcNow))
        {
            yield return new ValidationResult(
                "Neste gang må være etter i dag.", [nameof(NesteDato)]);
        }
    }
}
