using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

public sealed class ForplanSideVm
{
    public int DyrId { get; set; }

    public string DyrNavn { get; set; } = string.Empty;

    public ForplanResultat Resultat { get; set; } = ForplanResultat.IngenPlan();

    public ForplanRad? Aktiv { get; set; }

    public int[] Maltider { get; set; } = [];

    public NyForplanVm Ny { get; set; } = new();
}
