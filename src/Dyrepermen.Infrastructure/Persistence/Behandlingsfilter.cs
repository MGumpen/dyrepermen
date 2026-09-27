using System.Linq.Expressions;
using Dyrepermen.Domain.Entities;

namespace Dyrepermen.Infrastructure.Persistence;

/// <summary>
/// Nar en behandlings "neste gang" fortsatt venter.
///
/// En paminnelse er fulgt opp nar samme dyr har fatt en nyere behandling av
/// samme type og med samme preparat - enten ved at noen krysset av "gitt",
/// eller ved at den ble registrert pa vanlig mate. Uten denne regelen ble
/// forrige ormekur liggende som "forfalt" pa dashbordet for alltid, selv om
/// den nye var gitt for lengst.
///
/// Preparatet er med i noklen, ikke bare typen. En hund kan ha to vaksiner
/// med hver sin syklus - kennelhoste hvert ar, grunnvaksine hvert tredje - og
/// da skal den ene ikke skjule den andre. Prisen er at et nytt preparat
/// for samme ormekur lar den gamle paminnelsen sta. Det er synlig og kan
/// rettes; en paminnelse som forsvinner i stillhet er det ikke. Se ADR 0016.
///
/// Ett uttrykk, brukt av dashbordet, dyrets side og behandlingssiden. Regelen
/// sto ellers tre steder, og tre steder spriker.
/// </summary>
public static class Behandlingsfilter
{
    public static readonly Expression<Func<Behandling, bool>> ApenPaminnelse =
        b => b.NesteDato != null
            && !b.Dyr.Behandlinger.Any(nyere =>
                nyere.Type == b.Type
                // Store og sma bokstaver teller ikke: "milbemax" er samme
                // preparat som "Milbemax". Tomt og null er det samme.
                && (nyere.Preparat ?? "").ToLower() == (b.Preparat ?? "").ToLower()
                && (nyere.Dato > b.Dato
                    || (nyere.Dato == b.Dato && nyere.Id > b.Id)));
}
