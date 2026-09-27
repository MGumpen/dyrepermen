namespace Dyrepermen.Application.Dtos;

/// <summary>
/// En behandling som venter pa neste gang, slik dyrets side viser den.
/// </summary>
public sealed record KommendeBehandling(
    int Id,
    string Tekst,
    DateOnly NesteDato,

    /// <summary>
    /// Gitt for i dag, og kan krysses av. Se Behandlingsintervall.KanGisIgjen.
    /// </summary>
    bool KanGis);
