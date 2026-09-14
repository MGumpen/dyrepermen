using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Interfaces;

/// <summary>
/// Hvem som er innlogget, hvilken husstand hun ser pa na, og med hvilken
/// rolle. Fylles av HusstandMiddleware i samme oppslag som setter HusstandId.
///
/// Rollen gjelder den AKTIVE husstanden. Du kan vaere eier i din egen og
/// gjest i din fars, sa den kan endre seg nar du bytter. Se ADR 0009.
/// </summary>
public interface IGjeldendeBruker
{
    int? BrukerId { get; }

    string Visningsnavn { get; }

    string Epost { get; }

    string HusstandNavn { get; }

    Husstandsrolle Rolle { get; }

    /// <summary>
    /// Gjest kan lese alt og logge det daglige, men ikke endre dyr,
    /// medlemmer eller innstillinger.
    /// </summary>
    bool KanEndre { get; }

    IReadOnlyList<HusstandsValg> Husstander { get; }

    bool ErInnlogget { get; }
}
