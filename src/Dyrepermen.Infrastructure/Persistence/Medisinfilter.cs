using System.Linq.Expressions;
using Dyrepermen.Domain.Entities;

namespace Dyrepermen.Infrastructure.Persistence;

/// <summary>
/// Nar en medisin er aktiv: ikke avsluttet, og sluttdatoen - som er til og
/// med - har ikke passert. Ett uttrykk for dashbordet, dyrets side og
/// informasjonssiden. Regelen sto tre steder, og da den fikk et vilkar til,
/// ville den ellers blitt rettet to av tre. Se ADR 0017.
///
/// Legg uttrykket i en lokal variabel for sporringen
/// (<c>var aktiv = Medisinfilter.Aktiv(idag);</c>) og bruk variabelen inne i
/// Select. Da ser EF et ferdig uttrykk, ikke et metodekall det ma tolke.
///
/// Samme regel i minnet: MedisinRad.ErAvsluttet. Endres den ene, ma den
/// andre folge med.
/// </summary>
public static class Medisinfilter
{
    public static Expression<Func<Medisin, bool>> Aktiv(DateOnly idag)
        => m => m.AvsluttetTid == null
            && (m.SluttDato == null || m.SluttDato >= idag);
}
