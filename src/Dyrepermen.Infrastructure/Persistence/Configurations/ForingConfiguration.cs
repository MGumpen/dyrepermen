using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class ForingConfiguration : IEntityTypeConfiguration<Foring>
{
    public void Configure(EntityTypeBuilder<Foring> b)
    {
        b.HasKey(f => f.Id);
        b.Property(f => f.Id).UseIdentityAlwaysColumn();

        b.Property(f => f.Kommentar).HasMaxLength(200);
        b.Property(f => f.Fornavn).HasMaxLength(80);

        // Ingen HasDefaultValue her, med vilje. Settes en standardverdi i
        // modellen, utelater EF kolonnen nar verdien er lik CLR-standarden -
        // og da hviler riktigheten pa at de to alltid er like. Det var
        // nettopp den fellen HusstandInvitasjon.Rolle gikk i. Uten
        // standardverdi sender EF alltid typen eksplisitt.
        b.Property(f => f.Type)
         .HasConversion(
             v => v == Foringstype.Godbit ? 'G' : 'M',
             v => v == 'G' ? Foringstype.Godbit : Foringstype.Maltid)
         .HasColumnType("char(1)")
         .IsRequired();

        b.Property(f => f.Tidspunkt)
         .HasDefaultValueSql("now()")
         .IsRequired();

        b.HasOne(f => f.Dyr)
         .WithMany(d => d.Foringer)
         .HasForeignKey(f => f.DyrId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(f => f.GittAv)
         .WithMany()
         .HasForeignKey(f => f.GittAvBrukerId)
         .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(f => new { f.DyrId, f.Tidspunkt })
         .IsDescending(false, true)
         .HasDatabaseName("ix_foring_dyr_tid");

        b.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_foring_mengde", "mengde_gram IS NULL OR mengde_gram > 0");

            t.HasCheckConstraint("ck_foring_type", "type IN ('M','G')");
        });
    }
}
