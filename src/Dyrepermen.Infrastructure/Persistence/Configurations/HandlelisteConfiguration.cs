using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class HandlelisteConfiguration : IEntityTypeConfiguration<Handleliste>
{
    public void Configure(EntityTypeBuilder<Handleliste> b)
    {
        b.HasKey(h => h.Id);
        b.Property(h => h.Id).UseIdentityAlwaysColumn();

        b.Property(h => h.Tekst).HasMaxLength(120).IsRequired();
        b.Property(h => h.Antall).HasDefaultValue(1);

        b.Property(h => h.OpprettetDato)
         .HasDefaultValueSql("CURRENT_DATE")
         .IsRequired();

        b.Property(h => h.Status)
         .HasConversion(
             v => v == HandlelisteStatus.Aktiv ? 'A' : 'K',
             v => v == 'A' ? HandlelisteStatus.Aktiv : HandlelisteStatus.Kjopt)
         .HasColumnType("char(1)")
         .HasDefaultValue(HandlelisteStatus.Aktiv)
         .IsRequired();

        // Henger pa husstanden, ikke pa dyret. Ingen cascade fra husstand.
        b.HasOne(h => h.Husstand)
         .WithMany(x => x.Handleliste)
         .HasForeignKey(h => h.HusstandId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(h => h.Dyr)
         .WithMany()
         .HasForeignKey(h => h.DyrId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(h => h.OpprettetAv)
         .WithMany()
         .HasForeignKey(h => h.OpprettetAvBrukerId)
         .OnDelete(DeleteBehavior.SetNull);

        // Partiell indeks - forsiden leser kun aktive punkter.
        b.HasIndex(h => h.HusstandId)
         .HasFilter("status = 'A'")
         .HasDatabaseName("ix_handleliste_aktiv");

        b.ToTable(t => t.HasCheckConstraint(
            "ck_handleliste_status", "status IN ('A','K')"));
    }
}
