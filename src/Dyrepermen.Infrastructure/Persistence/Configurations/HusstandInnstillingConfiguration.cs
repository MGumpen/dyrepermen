using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class HusstandInnstillingConfiguration
    : IEntityTypeConfiguration<HusstandInnstilling>
{
    public void Configure(EntityTypeBuilder<HusstandInnstilling> b)
    {
        // Bade primaernokkel og fremmednokkel - skal ikke genereres.
        b.HasKey(i => i.HusstandId);
        b.Property(i => i.HusstandId).ValueGeneratedNever();

        b.Property(i => i.ForingsloggStandard).HasDefaultValue(false);
        b.Property(i => i.ForplanStandard).HasDefaultValue(true);
        b.Property(i => i.VarslerAktiv).HasDefaultValue(true);

        b.HasOne(i => i.Husstand)
         .WithOne(h => h.Innstilling)
         .HasForeignKey<HusstandInnstilling>(i => i.HusstandId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
