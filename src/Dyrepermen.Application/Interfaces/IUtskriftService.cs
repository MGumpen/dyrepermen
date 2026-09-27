using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IUtskriftService
{
    /// <summary>
    /// Det som er valgt, om de valgte dyrene, i ett kall.
    ///
    /// Deler som ikke er valgt, hentes ikke - de koster ingen rundtur. Antall
    /// sporringer vokser fortsatt ikke med antall dyr.
    /// </summary>
    Task<Utskrift> Hent(Utskriftsvalg valg, CancellationToken ct);
}
