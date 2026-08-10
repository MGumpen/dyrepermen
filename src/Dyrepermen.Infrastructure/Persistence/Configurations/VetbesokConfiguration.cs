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
        b.Property(x => x.KostnadKr).HasDefaultValue(0);
        b.Property(x => x.ForsikringKrevd).HasDefaultValue(false);

        b.HasOne(x => x.Dyr)
         .WithMany(d => d.Vetbesok)
         .HasForeignKey(x => x.DyrId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
