using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

/// <summary>
/// Filer lagt ved et veterinaerbesok - typisk kvitteringen. Filene ligger i
/// databasen, ikke pa disk. Se ADR 0018.
/// </summary>
public interface IVedleggService
{
    /// <summary>
    /// Kan filene legges ved? Null betyr ja; ellers en hel setning som sier
    /// hvorfor ikke. Sjekker filtype og storrelse, plassen husstanden og
    /// databasen har igjen, og at brukeren ikke er i demoen.
    ///
    /// Controlleren kaller denne for timen lagres, sa en ugyldig fil ikke gir
    /// en time uten den kvitteringen brukeren trodde var med.
    /// </summary>
    Task<string?> Kontroller(IReadOnlyList<NyttVedlegg> filer, CancellationToken ct);

    /// <summary>
    /// Legger filene ved besoket. Sjekker det samme som
    /// <see cref="Kontroller"/> en gang til, siden plassen kan vaere brukt
    /// opp i mellomtiden.
    /// </summary>
    Task<Vedleggsresultat> LeggVedBesok(
        int besokId, IReadOnlyList<NyttVedlegg> filer, CancellationToken ct);

    /// <summary>Null betyr at vedlegget ikke finnes i denne husstanden.</summary>
    Task<Vedleggsfil?> Hent(int dokumentId, CancellationToken ct);

    Task<bool> Slett(int dokumentId, CancellationToken ct);
}
