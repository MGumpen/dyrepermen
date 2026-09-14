using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class BrukerConfiguration : IEntityTypeConfiguration<Bruker>
{
    public void Configure(EntityTypeBuilder<Bruker> b)
    {
        b.Property(u => u.Visningsnavn).HasMaxLength(60).IsRequired();

    }
}
