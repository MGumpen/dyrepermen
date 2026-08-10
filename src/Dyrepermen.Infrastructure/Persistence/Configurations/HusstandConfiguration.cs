using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class HusstandConfiguration : IEntityTypeConfiguration<Husstand>
{
    public void Configure(EntityTypeBuilder<Husstand> b)
    {
        b.HasKey(h => h.Id);
        b.Property(h => h.Id).UseIdentityAlwaysColumn();

        b.Property(h => h.Navn).HasMaxLength(80).IsRequired();

        b.Property(h => h.OpprettetDato)
         .HasDefaultValueSql("CURRENT_DATE")
         .IsRequired();
    }
}
