using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

/// <summary>
/// Alle metodene er husstandsavgrenset gjennom det globale query-filteret.
/// Et dyr i en annen husstand oppleves som ikke-eksisterende.
/// </summary>
public interface IDyrService
{
    Task<IReadOnlyList<DyrListeElement>> HentAlle(CancellationToken ct);

    Task<DyrDetaljer?> HentDetaljer(int dyrId, CancellationToken ct);

    Task<DyrResultat> Opprett(NyttDyr input, CancellationToken ct);

    Task<DyrResultat> Oppdater(RedigerDyr input, CancellationToken ct);

    Task<bool> Deaktiver(int dyrId, CancellationToken ct);
}
