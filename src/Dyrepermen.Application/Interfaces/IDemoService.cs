namespace Dyrepermen.Application.Interfaces;

/// <summary>
/// Demohusstander for besokende uten konto. Se ADR 0015.
/// </summary>
public interface IDemoService
{
    /// <summary>
    /// Rydder bort utlopte demoer, og oppretter en demobruker med en fersk
    /// demohusstand. Gir bruker-ID-en, eller <c>null</c> nar taket pa aktive
    /// demoer er nadd.
    /// </summary>
    Task<int?> Start(CancellationToken ct);

    /// <summary>
    /// Sletter demoen straks, ved utlogging. Gjor ingenting hvis brukeren
    /// ikke er en demobruker.
    /// </summary>
    Task Avslutt(int brukerId, CancellationToken ct);
}
