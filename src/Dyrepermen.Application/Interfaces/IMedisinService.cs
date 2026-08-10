using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IMedisinService
{
    Task<IReadOnlyList<MedisinRad>> HentFor(int dyrId, CancellationToken ct);

    Task<bool> Registrer(NyMedisin input, CancellationToken ct);

    /// <summary>Setter sluttdato til i dag. Raden og doseloggen beholdes.</summary>
    Task<bool> Avslutt(int dyrId, int medisinId, CancellationToken ct);

    /// <summary>
    /// Sjekken mot forrige dose ligger her, ikke i controlleren.
    /// <paramref name="bekreftet"/> lar brukeren overstyre bevisst.
    /// </summary>
    Task<DoseResultat> LoggDose(
        int dyrId,
        int medisinId,
        int? brukerId,
        bool bekreftet,
        CancellationToken ct);
}
