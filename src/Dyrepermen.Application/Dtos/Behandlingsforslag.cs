using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

/// <summary>
/// En behandling husstanden har gitt for, slik at den kan velges igjen.
///
/// Siste forekomst av hver kombinasjon av type og preparat, pa tvers av
/// dyrene. Datoene er med fordi intervallet folger med nar forslaget brukes:
/// ble ormekuren sist gitt med tre maneder til neste, foreslas tre maneder
/// igjen.
/// </summary>
public sealed record Behandlingsforslag(
    int Id,
    BehandlingType Type,
    string? Preparat,
    DateOnly Dato,
    DateOnly? NesteDato);
