using Dyrepermen.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Infrastructure.Persistence;

/// <summary>
/// Gir Identity-tabellene snake_case-navn, slik plan kapittel 5.1 krever.
///
/// UseSnakeCaseNamingConvention rorer dem ikke av seg selv: IdentityDbContext
/// setter navnene eksplisitt med ToTable("AspNetUsers"), og konvensjonen
/// respekterer eksplisitt konfigurasjon. Uten dette blir tabellene hetende
/// "AspNetUsers" med store bokstaver og anforselstegn, mens domenetabellene
/// heter dyr og vekt. Kolonnene inni blir riktige uansett - de er ikke
/// eksplisitt navngitt.
///
/// Samme gjelder de tre indeksene Identity navngir selv.
/// </summary>
internal static class IdentityTabellnavn
{
    public static void BrukSnakeCase(this ModelBuilder b)
    {
        b.Entity<Bruker>(e =>
        {
            e.ToTable("asp_net_users");
            e.HasIndex(u => u.NormalizedEmail).HasDatabaseName("ix_asp_net_users_epost");
            e.HasIndex(u => u.NormalizedUserName).HasDatabaseName("ux_asp_net_users_brukernavn");
        });

        b.Entity<IdentityRole<int>>(e =>
        {
            e.ToTable("asp_net_roles");
            e.HasIndex(r => r.NormalizedName).HasDatabaseName("ux_asp_net_roles_navn");
        });

        b.Entity<IdentityUserClaim<int>>().ToTable("asp_net_user_claims");
        b.Entity<IdentityUserLogin<int>>().ToTable("asp_net_user_logins");
        b.Entity<IdentityUserToken<int>>().ToTable("asp_net_user_tokens");
        b.Entity<IdentityUserRole<int>>().ToTable("asp_net_user_roles");
        b.Entity<IdentityRoleClaim<int>>().ToTable("asp_net_role_claims");
    }
}
