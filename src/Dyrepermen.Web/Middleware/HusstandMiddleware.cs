using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Services;
using Dyrepermen.Infrastructure.Persistence;
using Dyrepermen.Web.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Web.Middleware;

/// <summary>
/// Fyller husstandskonteksten for foresporselen, og sender brukere uten
/// husstand til oppsettsiden.
///
/// Planen kapittel 12.1 leste husstand fra en claim - det er nettopp det
/// kapittel 12.3.1 sier man ikke skal gjore, fordi claim-en blir foreldet
/// nar noen andre legger deg til i en husstand. Se ADR 0001.
///
/// Med flere husstander per bruker (ADR 0009) gjor middlewaren i tillegg to
/// ting: den finner alle medlemskapene, og velger hvilket som er aktivt.
/// </summary>
public sealed class HusstandMiddleware
{
    /// <summary>
    /// Hvilken husstand brukeren ser pa. Kun et valg, ikke en rettighet -
    /// verdien valideres mot medlemskapene ved hver eneste foresporsel.
    /// En manipulert kapsel gir derfor ingen tilgang.
    /// </summary>
    public const string Kapsel = "dyrepermen_husstand";

    private static readonly string[] Unntak =
    [
        "/husstand/oppsett",
        // MA vaere med. Planen kapittel 12.1 lister bare /husstand/oppsett,
        // men skjemaet der poster til /husstand/opprett. Uten unntaket blir
        // POST-en omdirigert av denne middlewaren for den nar controlleren -
        // brukeren kommer aldri ut av oppsettsiden.
        "/husstand/opprett",
        "/husstand/bytt",
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

        var bruker = await db.Users
            .Where(u => u.Id == brukerId.Value)
            .Select(u => new { u.Visningsnavn, u.Email })
            .FirstOrDefaultAsync(ctx.RequestAborted);

        kontekst.BrukerId = brukerId;
        kontekst.Visningsnavn = bruker?.Visningsnavn ?? "";
        kontekst.Epost = bruker?.Email ?? "";

        // Alle husstander brukeren er med i, med rolle. Ett oppslag.
        var medlemskap = await db.Husstandsmedlemskap
            .Where(m => m.BrukerId == brukerId.Value)
            .OrderBy(m => m.Husstand.Navn)
            .Select(m => new
            {
                m.HusstandId,
                Navn = m.Husstand.Navn,
                m.Rolle
            })
            .ToListAsync(ctx.RequestAborted);

        if (medlemskap.Count == 0)
        {
            kontekst.HusstandId = 0;
            kontekst.Husstander = [];

            if (!ErUnntak(ctx.Request.Path.Value ?? "/"))
            {
                ctx.Response.Redirect("/husstand/oppsett");
                return;
            }

            await _neste(ctx);
            return;
        }

        // Valget fra kapselen godtas KUN hvis det finnes et medlemskap.
        // Uten den valideringen ville en redigert kapsel gitt tilgang til en
        // vilkarlig husstand - hele tenant-isolasjonen henger i denne linja.
        var onsket = int.TryParse(ctx.Request.Cookies[Kapsel], out var fraKapsel)
            ? fraKapsel
            : 0;

        var aktiv = medlemskap.FirstOrDefault(m => m.HusstandId == onsket)
                    ?? medlemskap[0];

        kontekst.HusstandId = aktiv.HusstandId;
        kontekst.HusstandNavn = aktiv.Navn;
        kontekst.Rolle = aktiv.Rolle;
        kontekst.Husstander = medlemskap
            .Select(m => new HusstandsValg(
                m.HusstandId, m.Navn, m.Rolle, m.HusstandId == aktiv.HusstandId))
            .ToList();

        await _neste(ctx);
    }

    private static bool ErUnntak(string sti)
        => Unntak.Any(u => sti.StartsWith(u, StringComparison.OrdinalIgnoreCase));
}
