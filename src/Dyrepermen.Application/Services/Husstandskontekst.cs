using Dyrepermen.Application.Interfaces;

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
/// Bakgrunnsjobber og tester setter verdiene direkte.
/// </summary>
public sealed class Husstandskontekst : IHusstandContext, IGjeldendeBruker
{
    public int HusstandId { get; set; }

    public int? BrukerId { get; set; }

    public string Visningsnavn { get; set; } = string.Empty;

    public string Epost { get; set; } = string.Empty;

    public string HusstandNavn { get; set; } = string.Empty;

    public bool ErInnlogget => BrukerId is not null;
}
