using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Innholdet i en behandling slik brukeren har fylt det ut. Samme type
/// brukes bade nar en behandling registreres og nar den rettes - feltene er
/// de samme, og to nesten like typer ville drevet fra hverandre.
/// </summary>
public sealed record Behandlingsinnhold(
    int DyrId,
    BehandlingType Type,
    string? Preparat,
    DateOnly Dato,
    DateOnly? NesteDato,
    string? Notat);
