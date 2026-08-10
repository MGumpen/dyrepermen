namespace Dyrepermen.Application.Interfaces;

public interface IHusstandService
{
    /// <summary>
    /// Oppretter husstand med tilhorende innstillingsrad, og knytter brukeren
    /// til den. Returnerer husstandens ID.
    /// </summary>
    Task<int> OpprettHusstand(string navn, int brukerId, CancellationToken ct);
}
