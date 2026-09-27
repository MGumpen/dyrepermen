using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

/// <summary>
/// Veterinaerer og timer. Begge horer til samme side, men til hver sin
/// tabell: stedet horer til husstanden, besoket til dyret.
/// </summary>
public interface IVeterinarService
{
    /// <summary>
    /// Sortert etter type: fast forst, sa vakt, sa sykehus. Nar noe har
    /// skjedd og du apner siden, er det ikke fastveterinaeren du leter etter
    /// - men resten av tiden er det den du vil se oeverst.
    /// </summary>
    Task<IReadOnlyList<Veterinarrad>> Hent(CancellationToken ct);

    Task<Veterinarrad?> HentEn(int veterinarId, CancellationToken ct);

    /// <summary>False betyr tomt navn.</summary>
    Task<bool> Opprett(NyVeterinar input, CancellationToken ct);

    Task<bool> Oppdater(int veterinarId, NyVeterinar input, CancellationToken ct);

    /// <summary>
    /// Besokene beholdes med NULL i koblingen. Historikken skal ikke
    /// forsvinne fordi klinikken ble fjernet fra lista.
    /// </summary>
    Task<bool> Slett(int veterinarId, CancellationToken ct);

    /// <summary>Alle timer i husstanden, nyeste dato forst.</summary>
    Task<IReadOnlyList<Vetbesokrad>> HentBesok(CancellationToken ct);

    /// <summary>Null betyr at besoket ikke finnes i denne husstanden.</summary>
    Task<Vetbesokrad?> HentEttBesok(int besokId, CancellationToken ct);

    /// <summary>
    /// Id-en til det nye besoket, slik at vedlegg kan legges ved. Null betyr
    /// at dyret eller stedet horer til en annen husstand, eller at arsaken
    /// er tom.
    /// </summary>
    Task<int?> OpprettBesok(NyttVetbesok input, CancellationToken ct);

    /// <summary>
    /// Legger inn timen for en avtalt kontroll, og fjerner paminnelsen fra
    /// besoket den kom fra i samme lagring. Kontrollen star videre som den
    /// nye timen - to rader om samme avtale ville gitt dobbelt varsel.
    ///
    /// Horer <paramref name="fraBesokId"/> til et annet dyr eller en annen
    /// husstand, lagres timen likevel, men ingenting annet endres.
    /// Se ADR 0016.
    ///
    /// Returnerer id-en til den nye timen, som <see cref="OpprettBesok"/>.
    /// </summary>
    Task<int?> BestillKontroll(
        int fraBesokId, NyttVetbesok input, CancellationToken ct);

    Task<bool> OppdaterBesok(int besokId, NyttVetbesok input, CancellationToken ct);

    Task<bool> SlettBesok(int besokId, CancellationToken ct);
}
