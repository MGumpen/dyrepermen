using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IForingService
{
    Task<IReadOnlyList<ForingRad>> HentFor(int dyrId, CancellationToken ct);

    /// <summary>
    /// False betyr at dyret ikke finnes i husstanden, ELLER at
    /// foringsloggen er slatt av for det. Bryteren styrer visning og
    /// tilgang - en gammel faneside skal ikke kunne skrive til en avslatt
    /// funksjon. Se plan kapittel 8.2.
    /// </summary>
    Task<bool> Registrer(NyForing input, CancellationToken ct);

    /// <summary>
    /// Korrigering i etterkant. Glemmer man a huke av til man kommer hjem om
    /// kvelden, er automatikken feil og ma kunne overstyres.
    /// </summary>
    Task<bool> RedigerTid(
        int dyrId, int foringId, DateTimeOffset tidspunkt, CancellationToken ct);

    Task<bool> Slett(int dyrId, int foringId, CancellationToken ct);
}
