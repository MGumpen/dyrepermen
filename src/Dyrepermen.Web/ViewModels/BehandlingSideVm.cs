using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

public sealed class BehandlingSideVm
{
    public int DyrId { get; set; }

    public string DyrNavn { get; set; } = string.Empty;

    public IReadOnlyList<BehandlingRad> Historikk { get; set; } = [];

    /// <summary>
    /// Behandlingen skjemaet endrer, eller null nar det registrerer en ny.
    /// </summary>
    public int? RedigerId { get; set; }

    public NyBehandlingVm Ny { get; set; } = new();

    /// <summary>
    /// Det husstanden har gitt for. Tegnes som snarveier over skjemaet og
    /// som forslag i preparatfeltet.
    /// </summary>
    public IReadOnlyList<Behandlingsforslag> Forslag { get; set; } = [];

    /// <summary>Skjemaet er fylt ut fra et forslag, og datoene bor sjekkes.</summary>
    public bool FraForslag { get; set; }
}
