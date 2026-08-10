using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class BrukerConfiguration : IEntityTypeConfiguration<Bruker>
{
    public void Configure(EntityTypeBuilder<Bruker> b)
    {
        b.Property(u => u.Visningsnavn).HasMaxLength(60).IsRequired();

        // Restrict, ikke Cascade. Sletting av en husstand skal aldri ta med seg
        // brukerkontoer. Ved kontosletting slettes brukeren forst, deretter
        // husstanden - se plan kapittel 12.5.
        b.HasOne(u => u.Husstand)
         .WithMany(h => h.Medlemmer)
         .HasForeignKey(u => u.HusstandId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(u => u.HusstandId).HasDatabaseName("ix_bruker_husstand");
    }
}
