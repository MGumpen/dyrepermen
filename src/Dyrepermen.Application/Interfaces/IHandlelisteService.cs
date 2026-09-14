using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IHandlelisteService
{
    /// <summary>Aktive forst, deretter kjopte. Begge eldst forst.</summary>
    Task<IReadOnlyList<HandlelisteRad>> Hent(CancellationToken ct);

    /// <summary>De fem oeverste aktive, til dashbordet.</summary>
    Task<IReadOnlyList<HandlelisteRad>> HentAktive(int antall, CancellationToken ct);

    Task<bool> Legg(NyttPunkt input, CancellationToken ct);

    /// <summary>Veksler mellom aktiv og kjopt.</summary>
    Task<bool> VekslStatus(int punktId, CancellationToken ct);

    Task<bool> Slett(int punktId, CancellationToken ct);

    /// <summary>Fjerner alle kjopte punkter.</summary>
    Task<int> RyddKjopte(CancellationToken ct);
}
