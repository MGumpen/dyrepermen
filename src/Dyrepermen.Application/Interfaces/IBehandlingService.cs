using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IBehandlingService
{
    /// <summary>Synkende datorekkefolge - nyeste behandling forst.</summary>
    Task<IReadOnlyList<BehandlingRad>> HentFor(int dyrId, CancellationToken ct);

    /// <summary>False betyr at dyret ikke finnes i denne husstanden.</summary>
    Task<bool> Registrer(Behandlingsinnhold input, CancellationToken ct);

    /// <summary>
    /// Retter en behandling som allerede er registrert.
    ///
    /// "Neste gang" er ikke et faktum om fortiden, det er en avtale om
    /// framtiden - og den flyttes. Sier veterinaeren at ormekuren kan vente,
    /// skal datoen kunne endres uten at behandlingen som faktisk ble gitt ma
    /// slettes og legges inn pa nytt. Se ADR 0014.
    ///
    /// False betyr at behandlingen ikke finnes pa dette dyret i denne
    /// husstanden.
    /// </summary>
    Task<bool> Oppdater(
        int behandlingId, Behandlingsinnhold input, CancellationToken ct);

    Task<bool> Slett(int dyrId, int behandlingId, CancellationToken ct);

    /// <summary>
    /// Krysser av en paminnelse: registrerer en ny behandling av samme type
    /// og preparat med dagens dato, og neste gang med samme intervall som
    /// forrige. Den gamle raden blir staende urort - den er historikk, og
    /// paminnelsen forsvinner fordi en nyere behandling na finnes.
    /// Se ADR 0016.
    /// </summary>
    Task<GittResultat> Gitt(int dyrId, int behandlingId, CancellationToken ct);

    /// <summary>
    /// Behandlingene husstanden har gitt for, nyeste forst, en per
    /// kombinasjon av type og preparat. Pa tvers av dyrene: har den ene
    /// hunden fatt Milbemax, er det et godt forslag for den andre ogsa.
    /// </summary>
    Task<IReadOnlyList<Behandlingsforslag>> HentForslag(CancellationToken ct);
}
