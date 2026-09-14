using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IInformasjonService
{
    Task<IReadOnlyList<InformasjonRad>> Hent(CancellationToken ct);

    Task<IReadOnlyList<InformasjonRad>> HentNyeste(int antall, CancellationToken ct);

    Task<InformasjonRad?> HentEn(int id, CancellationToken ct);

    /// <summary>Oppretter nar Id er null, oppdaterer ellers.</summary>
    Task<bool> Lagre(NyInformasjon input, CancellationToken ct);

    Task<bool> Slett(int id, CancellationToken ct);

    /// <summary>
    /// Samleoversikt over alle aktive dyr, med notatene deres.
    /// Alt pa ett sted - en side du kan vise fram til dyrepasseren.
    /// </summary>
    Task<IReadOnlyList<DyreOversikt>> HentDyreoversikt(CancellationToken ct);
}
