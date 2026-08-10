using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class ForsikringConfiguration : IEntityTypeConfiguration<Forsikring>
{
    public void Configure(EntityTypeBuilder<Forsikring> b)
    {
        b.HasKey(f => f.Id);
        b.Property(f => f.Id).UseIdentityAlwaysColumn();

        b.Property(f => f.Selskap).HasMaxLength(80).IsRequired();
        b.Property(f => f.PoliseNr).HasMaxLength(40).IsRequired();
        b.Property(f => f.ArspremieKr).HasDefaultValue(0);
        b.Property(f => f.Egenandel).HasDefaultValue(0);

        b.HasOne(f => f.Dyr)
         .WithMany(d => d.Forsikringer)
         .HasForeignKey(f => f.DyrId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
