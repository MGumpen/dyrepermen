using Dyrepermen.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Dyrepermen.Web;

/// <summary>
/// Krever minst en stor bokstav - og regner Æ, Ø og Å som store bokstaver.
///
/// Identity sin egen RequireUppercase gjor IKKE det. Den bruker en
/// ASCII-sjekk, ikke char.IsUpper:
///
///     c >= 'A' &amp;&amp; c &lt;= 'Z'
///
/// I en norsk app betyr det at "Ørnulf7" avvises med beskjeden "passordet
/// ma inneholde minst en stor bokstav", mens brukeren sitter og ser pa en
/// stor bokstav. Det er samme klasse feil som den vi kom fra: regelen som
/// vises er ikke regelen som handheves.
///
/// Identity sin RequireUppercase er derfor slatt AV i Program.cs, og denne
/// validatoren gjor jobben i stedet. Feilkoden er den samme, sa oversettelsen
/// i KontoController dekker begge.
/// </summary>
public sealed class StorBokstavValidator : IPasswordValidator<Bruker>
{
    public Task<IdentityResult> ValidateAsync(
        UserManager<Bruker> forvalter, Bruker bruker, string? passord)
    {
        if (!Passordkrav.KreverStorBokstav
            || (passord is not null && passord.Any(char.IsUpper)))
        {
            return Task.FromResult(IdentityResult.Success);
        }

        return Task.FromResult(IdentityResult.Failed(new IdentityError
        {
            Code = "PasswordRequiresUpper",
            Description = Passordkrav.ManglerStorBokstav
        }));
    }
}
