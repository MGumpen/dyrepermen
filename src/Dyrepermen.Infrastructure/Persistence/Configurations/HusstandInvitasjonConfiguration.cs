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
             v => v == Husstandsrolle.Beboer ? 'B' : 'G',
             v => v == 'B' ? Husstandsrolle.Beboer : Husstandsrolle.Gjest)
         .HasColumnType("char(1)")
         // INGEN HasDefaultValue her. Beboer er CLR-standardverdien for
         // enumen, sa EF ville utelatt kolonnen fra INSERT nar rollen er
         // Beboer - og databasens standard 'G' ville slatt inn. Da ble en
         // invitert beboer stille lagret som gjest.
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
