using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class DoseConfiguration : IEntityTypeConfiguration<Dose>
{
    public void Configure(EntityTypeBuilder<Dose> b)
    {
        b.HasKey(d => d.Id);
        b.Property(d => d.Id).UseIdentityAlwaysColumn();

        b.HasOne(d => d.Medisin)
         .WithMany(m => m.Doser)
         .HasForeignKey(d => d.MedisinId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(d => d.GittAv)
         .WithMany()
         .HasForeignKey(d => d.GittAvBrukerId)
         .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(d => new { d.MedisinId, d.GittTid })
         .IsDescending(false, true)
         .HasDatabaseName("ix_dose_medisin_tid");
    }
}
