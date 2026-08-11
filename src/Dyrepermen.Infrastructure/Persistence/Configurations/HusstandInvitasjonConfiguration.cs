using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class HusstandInvitasjonConfiguration
    : IEntityTypeConfiguration<HusstandInvitasjon>
{
    public void Configure(EntityTypeBuilder<HusstandInvitasjon> b)
    {
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).UseIdentityAlwaysColumn();

        b.Property(i => i.Epost).HasMaxLength(256).IsRequired();

        b.Property(i => i.Rolle)
         .HasConversion(
             v => v == Husstandsrolle.Eier ? 'E' : 'G',
             v => v == 'E' ? Husstandsrolle.Eier : Husstandsrolle.Gjest)
         .HasColumnType("char(1)")
         .HasDefaultValue(Husstandsrolle.Gjest)
         .IsRequired();

        b.Property(i => i.OpprettetDato)
         .HasDefaultValueSql("CURRENT_DATE")
         .IsRequired();

        b.HasOne(i => i.Husstand)
         .WithMany(h => h.Invitasjoner)
         .HasForeignKey(i => i.HusstandId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(i => i.InnlostAv)
         .WithMany()
         .HasForeignKey(i => i.InnlostAvBrukerId)
         .OnDelete(DeleteBehavior.SetNull);

        b.HasOne(i => i.OpprettetAv)
         .WithMany()
         .HasForeignKey(i => i.OpprettetAvBrukerId)
         .OnDelete(DeleteBehavior.SetNull);

        // ux_invitasjon_epost er en funksjonell indeks pa lower(epost) og
        // legges inn som ra SQL i migrasjonen - EF Core kan ikke uttrykke den.
    }
}
