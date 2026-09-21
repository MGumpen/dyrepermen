using Dyrepermen.Domain.Entities;

namespace Dyrepermen.Application.Dtos;

/// <summary>
/// En ferdig demohusstand fra <see cref="Extensions.Demomal"/>.
///
/// Veterinarene og notatene star for seg fordi <see cref="Husstand"/> ikke
/// har noen samling for dem - de henger pa husstanden bare gjennom
/// fremmednokkelen. Alt annet nas fra husstanden.
/// </summary>
public sealed record Demodata(
    Husstand Husstand,
    IReadOnlyList<Veterinar> Veterinarer,
    IReadOnlyList<Informasjon> Informasjon);
