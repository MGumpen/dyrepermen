using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IBehandlingService
{
    /// <summary>Synkende datorekkefolge - nyeste behandling forst.</summary>
    Task<IReadOnlyList<BehandlingRad>> HentFor(int dyrId, CancellationToken ct);

    /// <summary>False betyr at dyret ikke finnes i denne husstanden.</summary>
    Task<bool> Registrer(NyBehandling input, CancellationToken ct);

    Task<bool> Slett(int dyrId, int behandlingId, CancellationToken ct);
}
