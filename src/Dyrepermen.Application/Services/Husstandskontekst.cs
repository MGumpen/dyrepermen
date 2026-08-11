using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Services;

/// <summary>
/// Scoped holder for gjeldende foresporsel.
///
/// Denne typen har bevisst INGEN avhengigheter. Planen kapittel 7.2 lot
/// implementasjonen ta inn DyrepermenDbContext, mens DbContext-en samtidig tar
/// inn IHusstandContext - en sirkulaer avhengighet som DI-containeren kaster pa
/// ved forste foresporsel. Se docs/beslutninger/0001.
///
/// Verdiene fylles av HusstandMiddleware i Web-laget, etter autentisering og
/// for noe leser dem. Star den urort, er HusstandId 0, og alle query-filtre
/// gir tomt resultatsett.
///
/// Rollen star ogsa her, og gjelder den AKTIVE husstanden. Standardverdien er
/// Gjest, ikke Eier: glipper middlewaren, skal man ha minst rettigheter -
/// ikke flest.
/// </summary>
public sealed class Husstandskontekst : IHusstandContext, IGjeldendeBruker
{
    public int HusstandId { get; set; }

    public int? BrukerId { get; set; }

    public string Visningsnavn { get; set; } = string.Empty;

    public string Epost { get; set; } = string.Empty;

    public string HusstandNavn { get; set; } = string.Empty;

    public Husstandsrolle Rolle { get; set; } = Husstandsrolle.Gjest;

    public bool KanEndre => Rolle == Husstandsrolle.Beboer;

    public IReadOnlyList<HusstandsValg> Husstander { get; set; } = [];

    public bool ErInnlogget => BrukerId is not null;
}
