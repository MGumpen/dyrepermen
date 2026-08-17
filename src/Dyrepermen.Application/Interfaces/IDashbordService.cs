using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IDashbordService
{
    /// <summary>
    /// Ytelseskrav: et fast antall databasesporringer, uansett antall dyr.
    /// I dag atte. Kravet er ikke tallet, men at det ikke vokser med raden -
    /// en ny kilde koster hoyst en fast rundtur, og helst ingen fordi den
    /// slas sammen med en sporring som allerede finnes.
    ///
    /// Dashbordet er den mest besokte siden, og databasen skalerer til null
    /// mellom okter. Se plan kapittel 10.3.
    /// </summary>
    Task<Dashbord> Hent(CancellationToken ct);
}
