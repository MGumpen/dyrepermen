using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

public sealed class MedisinSideVm
{
    public int DyrId { get; set; }

    public string DyrNavn { get; set; } = string.Empty;

    public IReadOnlyList<MedisinRad> Medisiner { get; set; } = [];

    public NyMedisinVm Ny { get; set; } = new();

    /// <summary>
    /// Medisinen skjemaet endrer, eller null nar det registrerer en ny.
    /// </summary>
    public int? RedigerId { get; set; }
}
