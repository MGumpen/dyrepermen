using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class MedisinConfiguration : IEntityTypeConfiguration<Medisin>
{
    public void Configure(EntityTypeBuilder<Medisin> b)
    {
        b.HasKey(m => m.Id);
        b.Property(m => m.Id).UseIdentityAlwaysColumn();

        b.Property(m => m.Navn).HasMaxLength(80).IsRequired();
        b.Property(m => m.Dose).HasMaxLength(40).IsRequired();
        b.Property(m => m.IntervallTimer).HasDefaultValue(0);

        b.HasOne(m => m.Dyr)
         .WithMany(d => d.Medisiner)
         .HasForeignKey(m => m.DyrId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
