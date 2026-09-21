using Dyrepermen.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dyrepermen.Web.Filtre;

/// <summary>
/// Avviser handlingen for demobrukere. Se ADR 0015 avsnitt 9.
///
/// Knappene er skjult i demoen, men skjulingen er bare pynt - dette er
/// sperren. Uten den kunne en anonym besokende lagt en ekte persons
/// e-postadresse inn i demohusstanden, eller sendt e-post gjennom
/// kontaktskjemaet med en falsk svaradresse.
///
/// DemoTester sjekker at attributtet star pa handlingene ADR-en lister.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class StengtIDemoAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var meg = context.HttpContext.RequestServices
            .GetRequiredService<IGjeldendeBruker>();

        if (meg.ErDemo)
        {
            context.Result = new ForbidResult();
        }
    }
}
