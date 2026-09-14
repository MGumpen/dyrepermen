using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Extensions;

/// <summary>
/// Visningsnavn for <see cref="Veterinartype"/>.
///
/// Ligger her og ikke i en visning fordi den samme switchen ellers ma skrives
/// pa nytt for hvert sted typen vises - listen, detaljkortet og dashbordet.
/// Tre kopier av samme regel er tre steder den kan komme i utakt.
/// </summary>
public static class Veterinarformat
{
    public static string Typenavn(Veterinartype type) => type switch
    {
        Veterinartype.Vakt => "Vakt",
        Veterinartype.Sykehus => "Dyresykehus",
        Veterinartype.Annet => "Annet",
        _ => "Fast veterinær"
    };
}
