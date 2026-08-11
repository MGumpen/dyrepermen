using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Enums;

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
    /// Navn husstanden allerede har brukt, nyeste forst. Fyller forslagene i
    /// foringsdialogen.
    /// </summary>
    Task<IReadOnlyList<string>> HentFornavn(
        Foringstype type, CancellationToken ct);

    /// <summary>
    /// Korrigering i etterkant. Glemmer man a huke av til man kommer hjem om
    /// kvelden, er automatikken feil og ma kunne overstyres.
    /// </summary>
    Task<bool> RedigerTid(
        int dyrId, int foringId, DateTimeOffset tidspunkt, CancellationToken ct);

    Task<bool> Slett(int dyrId, int foringId, CancellationToken ct);
}
