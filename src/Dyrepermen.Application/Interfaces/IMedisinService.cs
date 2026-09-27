using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IMedisinService
{
    Task<IReadOnlyList<MedisinRad>> HentFor(int dyrId, CancellationToken ct);

    Task<bool> Registrer(NyMedisin input, CancellationToken ct);

    /// <summary>
    /// Avslutter medisinen med en gang. Raden og doseloggen beholdes.
    /// Se ADR 0017.
    /// </summary>
    Task<bool> Avslutt(int dyrId, int medisinId, CancellationToken ct);

    /// <summary>
    /// Retter navn, dose, intervall og datoer - for eksempel nar dosen
    /// trappes ned. Doseloggen rores ikke. False betyr at medisinen ikke
    /// finnes pa dette dyret i denne husstanden.
    /// </summary>
    Task<bool> Oppdater(
        int dyrId, int medisinId, NyMedisin input, CancellationToken ct);

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
