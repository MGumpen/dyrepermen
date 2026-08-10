using Dyrepermen.Application.Interfaces;

namespace Dyrepermen.Application.Services;

/// <summary>
/// Scoped holder for gjeldende husstand.
///
/// Denne typen har bevisst INGEN avhengigheter. Planen kapittel 7.2 lot
/// implementasjonen ta inn DyrepermenDbContext, mens DbContext-en samtidig tar
/// inn IHusstandContext - en sirkulaer avhengighet som DI-containeren kaster pa
/// ved forste foresporsel. Se docs/beslutninger/0001.
///
/// Verdien fylles av HusstandMiddleware i Web-laget, etter autentisering og for
/// noe leser den. Star den urort, er HusstandId 0, og alle query-filtre gir
/// tomt resultatsett.
///
/// Bakgrunnsjobber og tester setter verdien direkte.
/// </summary>
public sealed class Husstandskontekst : IHusstandContext
{
    public int HusstandId { get; set; }
}
