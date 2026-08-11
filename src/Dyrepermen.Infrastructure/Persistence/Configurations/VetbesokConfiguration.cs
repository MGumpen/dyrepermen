using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class VetbesokConfiguration : IEntityTypeConfiguration<Vetbesok>
{
    public void Configure(EntityTypeBuilder<Vetbesok> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityAlwaysColumn();

        b.Property(x => x.Klinikk).HasMaxLength(100);
        b.Property(x => x.Arsak).HasMaxLength(200).IsRequired();
        b.Property(x => x.Diagnose).HasMaxLength(200);
        b.Property(x => x.Notat).HasMaxLength(500);
        b.Property(x => x.ForsikringKrevd).HasDefaultValue(false);

        b.HasOne(x => x.Dyr)
         .WithMany(d => d.Vetbesok)
         .HasForeignKey(x => x.DyrId)
         .OnDelete(DeleteBehavior.Cascade);

        // Slettes stedet, beholdes besoket med NULL. Historikken skal ikke
        // forsvinne fordi klinikken byttet navn eller ble fjernet fra lista.
        b.HasOne(x => x.Veterinar)
         .WithMany(v => v.Besok)
         .HasForeignKey(x => x.VeterinarId)
         .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => new { x.DyrId, x.Dato })
         .IsDescending(false, true)
         .HasDatabaseName("ix_vetbesok_dyr_dato");

        b.ToTable(t =>
        {
            // Nullbar med vilje - en kommende time har ingen pris enna. Men
            // star det et tall, skal det ikke vaere negativt.
            t.HasCheckConstraint(
                "ck_vetbesok_kostnad", "kostnad_kr IS NULL OR kostnad_kr >= 0");

            t.HasCheckConstraint(
                "ck_vetbesok_refundert",
                "refundert_kr IS NULL OR refundert_kr >= 0");

            // Refundert uten at forsikring er krevd er selvmotsigende, og
            // ville gitt et regnskap som ikke gar opp.
            t.HasCheckConstraint(
                "ck_vetbesok_refusjon_krever_krav",
                "refundert_kr IS NULL OR forsikring_krevd");
        });
    }
}
