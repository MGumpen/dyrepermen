using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IForsikringService
{
    Task<IReadOnlyList<ForsikringRad>> Hent(CancellationToken ct);

    Task<ForsikringRad?> HentEn(int id, CancellationToken ct);

    /// <summary>Oppretter nar Id er null, oppdaterer ellers.</summary>
    Task<bool> Lagre(NyForsikring input, CancellationToken ct);

    Task<bool> Slett(int id, CancellationToken ct);
}
