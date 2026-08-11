using Dyrepermen.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dyrepermen.Web.Filtre;

/// <summary>
/// Blokkerer handlingen for gjester i den aktive husstanden.
///
/// Rollen er ikke en claim - den avhenger av hvilken husstand du ser pa, og
/// kan endre seg nar du bytter. Derfor et filter som leser den fra
/// foresporselskonteksten, ikke en autorisasjonspolicy.
///
/// Regelen som gjelder: alt som ENDRER dyr, medlemmer, innstillinger eller
/// forsikring krever eier. Alt som LOGGER det daglige - vekt, foring, doser,
/// handleliste - er apent for gjester. Se ADR 0009.
///
/// Testen RolleTester i integrasjonsprosjektet gar gjennom alle POST-
/// handlinger og feiler hvis en ny mangler enten dette attributtet eller en
/// plass pa gjestelisten. Da kan ingen legge til en skrivehandling og glemme
/// tilgangskontrollen.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class KreverEierAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var meg = context.HttpContext.RequestServices
            .GetRequiredService<IGjeldendeBruker>();

        if (!meg.KanEndre)
        {
            // 403, ikke 404. Brukeren har lov til a se siden - hun har bare
            // ikke lov til a endre den, og da er det riktig a si det.
            context.Result = new ForbidResult();
        }
    }
}
