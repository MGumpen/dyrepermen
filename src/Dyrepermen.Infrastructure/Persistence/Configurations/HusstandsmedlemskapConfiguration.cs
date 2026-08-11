using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class HusstandsmedlemskapConfiguration
    : IEntityTypeConfiguration<Husstandsmedlemskap>
{
    public void Configure(EntityTypeBuilder<Husstandsmedlemskap> b)
    {
        b.HasKey(m => m.Id);
        b.Property(m => m.Id).UseIdentityAlwaysColumn();

        b.Property(m => m.Rolle)
         .HasConversion(
             v => v == Husstandsrolle.Beboer ? 'B' : 'G',
             v => v == 'B' ? Husstandsrolle.Beboer : Husstandsrolle.Gjest)
         .HasColumnType("char(1)")
         .IsRequired();

        b.Property(m => m.OpprettetDato)
         .HasDefaultValueSql("CURRENT_DATE")
         .IsRequired();

        b.HasOne(m => m.Husstand)
         .WithMany(h => h.Medlemskap)
         .HasForeignKey(m => m.HusstandId)
         .OnDelete(DeleteBehavior.Cascade);

        // Cascade fra bruker: slettes kontoen, forsvinner medlemskapene.
        // Husstandens DATA bestar - den koblingen gar via *_av_bruker_id
        // med SET NULL, ikke via denne tabellen. Se plan kapittel 12.5.
        b.HasOne(m => m.Bruker)
         .WithMany(u => u.Medlemskap)
         .HasForeignKey(m => m.BrukerId)
         .OnDelete(DeleteBehavior.Cascade);

        // En bruker kan vaere med i en husstand kun en gang.
        b.HasIndex(m => new { m.HusstandId, m.BrukerId })
         .IsUnique()
         .HasDatabaseName("ux_medlemskap_husstand_bruker");

        b.HasIndex(m => m.BrukerId).HasDatabaseName("ix_medlemskap_bruker");

        b.ToTable(t => t.HasCheckConstraint(
            "ck_medlemskap_rolle", "rolle IN ('B','G')"));
    }
}
