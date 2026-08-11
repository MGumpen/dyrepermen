using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

public sealed class ForingSideVm
{
    public int DyrId { get; set; }

    public string DyrNavn { get; set; } = string.Empty;

    public IReadOnlyList<ForingRad> Historikk { get; set; } = [];

    /// <summary>
    /// Foreslatt mengde per maltid fra den aktive forplanen. Null nar det
    /// ikke finnes plan, eller nar planen mangler vektgrunnlag - da skal
    /// feltet sta tomt, ikke vise 0.
    /// </summary>
    public int? ForeslattMengde { get; set; }

    public int AntallMaltider { get; set; }
}
