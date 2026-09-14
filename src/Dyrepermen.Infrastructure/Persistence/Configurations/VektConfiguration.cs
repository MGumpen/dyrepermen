using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class VektConfiguration : IEntityTypeConfiguration<Vekt>
{
    public void Configure(EntityTypeBuilder<Vekt> b)
    {
        b.HasKey(v => v.Id);
        b.Property(v => v.Id).UseIdentityAlwaysColumn();

        b.HasOne(v => v.Dyr)
         .WithMany(d => d.Vekter)
         .HasForeignKey(v => v.DyrId)
         .OnDelete(DeleteBehavior.Cascade);

        // SetNull, ikke Cascade. Sletting av en bruker skal avidentifisere
        // radene, ikke ta med seg hundens vekthistorikk. Plan kapittel 12.5.
        b.HasOne(v => v.RegistrertAv)
         .WithMany()
         .HasForeignKey(v => v.RegistrertAvBrukerId)
         .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(v => new { v.DyrId, v.Dato })
         .IsDescending(false, true)
         .HasDatabaseName("ix_vekt_dyr_dato");

        b.ToTable(t => t.HasCheckConstraint("ck_vekt_gram", "vekt_gram > 0"));
    }
}
