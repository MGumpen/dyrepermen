using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Infrastructure.Services;

/// <summary>
/// Sletting av brukere, ETT sted. Kontosletting og demoopprydding bruker
/// begge denne - to kopier sprikte forste gang noen la til en tabell.
/// Se ADR 0015 avsnitt 6.
/// </summary>
internal static class Brukersletting
{
    /// <summary>
    /// Sletter brukerne, og husstandene der de er eneste medlem, i en
    /// transaksjon. Gir antall husstander som ble slettet.
    /// </summary>
    public static async Task<int> SlettBrukere(
        this DyrepermenDbContext db,
        IReadOnlyCollection<int> brukerIder,
        CancellationToken ct)
    {
        // Husstander der brukeren er eneste medlem. De blir utilgjengelige for
        // alltid hvis de blir staende uten medlemmer, sa de slettes med.
        var alene = await db.Husstandsmedlemskap
            .Where(m => brukerIder.Contains(m.BrukerId))
            .Where(m => m.Husstand.Medlemskap.Count == 1)
            .Select(m => m.HusstandId)
            .ToListAsync(ct);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // ON DELETE SET NULL i skjemaet gjor avidentifiseringen: vektrader,
        // doser og foringer beholdes med null i *_av_bruker_id. Visningslaget
        // skriver "slettet bruker" der navnet sto. Identitys egne tabeller -
        // claims, innlogginger, tokens - folger med ved kaskade.
        //
        // En kaskadesletting ville tatt med seg hele vekthistorikken til
        // hunden fordi det tilfeldigvis var denne personen som registrerte
        // malingene. Det er feil, og det er ikke til a reversere.
        await db.Users
            .Where(u => brukerIder.Contains(u.Id))
            .ExecuteDeleteAsync(ct);

        // Rekkefolgen er bestemt av fremmednoklene. Handlelisten forst: den
        // peker pa dyret med RESTRICT, og star en vare igjen, stopper den
        // slettingen av dyrene. Dyrene tar med seg alt som henger pa dem, og
        // husstanden tar veterinarene, innstillingene, invitasjonene og
        // medlemskapene.
        await db.Handleliste.IgnoreQueryFilters()
            .Where(x => alene.Contains(x.HusstandId))
            .ExecuteDeleteAsync(ct);

        await db.Informasjon.IgnoreQueryFilters()
            .Where(x => alene.Contains(x.HusstandId))
            .ExecuteDeleteAsync(ct);

        await db.Dyr.IgnoreQueryFilters()
            .Where(d => alene.Contains(d.HusstandId))
            .ExecuteDeleteAsync(ct);

        await db.Husstand
            .Where(h => alene.Contains(h.Id))
            .ExecuteDeleteAsync(ct);

        await tx.CommitAsync(ct);

        return alene.Count;
    }
}
