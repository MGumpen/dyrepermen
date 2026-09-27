using Microsoft.AspNetCore.Mvc.Rendering;

namespace Dyrepermen.Web.ViewModels;

/// <summary>
/// Valgskjemaet for utskriften. Avkryssingsboksene er SelectListItem med
/// Selected satt, slik at skjemaet kan tegnes pa nytt med samme utvalg -
/// bade etter en feil og nar man kommer tilbake fra "Endre utvalg".
/// </summary>
public sealed class UtskriftsvalgVm
{
    public IReadOnlyList<SelectListItem> Dyr { get; init; } = [];

    public IReadOnlyList<SelectListItem> Deler { get; init; } = [];
}
