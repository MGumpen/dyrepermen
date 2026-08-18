using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IUtskriftService
{
    /// <summary>
    /// Alt om alle aktive dyr i husstanden, i ett kall.
    ///
    /// Ingen utvalg og ingen parametre: utskriften tar med alle dyrene, hver
    /// gang. Skal man vise permen til dyrepasseren, er det nettopp helheten
    /// som er poenget.
    /// </summary>
    Task<Utskrift> Hent(CancellationToken ct);
}
