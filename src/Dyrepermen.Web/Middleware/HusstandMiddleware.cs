using Dyrepermen.Application.Husstander;
using Dyrepermen.Infrastructure.Persistence;
using Dyrepermen.Web.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Web.Middleware;

/// <summary>
/// Fyller husstandskonteksten for foresporselen, og sender brukere uten
/// husstand til oppsettsiden.
///
/// Begge oppgavene her fordi de deler det samme oppslaget. Planen kapittel
/// 12.1 leste husstand fra en claim - det er nettopp det kapittel 12.3.1
/// sier man ikke skal gjore, fordi claim-en blir foreldet nar noen andre
/// legger deg til i en husstand. Se docs/beslutninger/0001.
/// </summary>
public sealed class HusstandMiddleware
{
    private static readonly string[] Unntak =
    [
        "/husstand/oppsett",
        // MA vaere med. Planen kapittel 12.1 lister bare /husstand/oppsett,
        // men skjemaet der poster til /husstand/opprett. Uten unntaket blir
        // POST-en omdirigert av denne middlewaren for den nar controlleren -
        // brukeren kommer aldri ut av oppsettsiden, og skjemaet ser ut til a
        // ikke gjore noe. Verifisert: uten linjen feiler oppsettsflyten.
        "/husstand/opprett",
        "/logg-ut",
        "/konto",
        "/helse"
    ];

    private readonly RequestDelegate _neste;

    public HusstandMiddleware(RequestDelegate neste) => _neste = neste;

    public async Task InvokeAsync(
        HttpContext ctx,
        DyrepermenDbContext db,
        Husstandskontekst kontekst)
    {
        var brukerId = ctx.User.Identity?.IsAuthenticated == true
            ? ctx.User.BrukerId()
            : null;

        if (brukerId is null)
        {
            // Uautentisert. HusstandId forblir 0, og alle query-filtre gir
            // tomt resultatsett - fail closed.
            await _neste(ctx);
            return;
        }

        // Ett indeksert primarnokkeloppslag per foresporsel. Ubetydelig
        // sammenlignet med klassen av feil claim-varianten gir.
        kontekst.HusstandId = await db.Users
            .Where(u => u.Id == brukerId.Value)
            .Select(u => u.HusstandId ?? 0)
            .FirstOrDefaultAsync(ctx.RequestAborted);

        var sti = ctx.Request.Path.Value ?? "/";

        if (kontekst.HusstandId == 0 && !ErUnntak(sti))
        {
            ctx.Response.Redirect("/husstand/oppsett");
            return;
        }

        await _neste(ctx);
    }

    private static bool ErUnntak(string sti)
        => Unntak.Any(u => sti.StartsWith(u, StringComparison.OrdinalIgnoreCase));
}
