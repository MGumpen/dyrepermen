using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IDashbordService
{
    /// <summary>
    /// Ytelseskrav: hoyst fire databasesporringer totalt, uansett antall dyr.
    /// Dashbordet er den mest besokte siden, og databasen skalerer til null
    /// mellom okter. Se plan kapittel 10.3.
    /// </summary>
    Task<Dashbord> Hent(CancellationToken ct);
}
