using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IVektService
{
    /// <summary>Synkende datorekkefolge - nyeste maling forst.</summary>
    Task<IReadOnlyList<VektRad>> HentFor(int dyrId, CancellationToken ct);

    /// <summary>False betyr at dyret ikke finnes i denne husstanden.</summary>
    Task<bool> Registrer(NyVekt input, CancellationToken ct);

    Task<bool> Slett(int dyrId, int vektId, CancellationToken ct);
}
