using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class BehandlingDoseConfiguration : IEntityTypeConfiguration<BehandlingDose>
{
    public void Configure(EntityTypeBuilder<BehandlingDose> b)
    {
        b.HasKey(d => d.Id);
        b.Property(d => d.Id).UseIdentityAlwaysColumn();

        b.HasOne(d => d.Behandling)
         .WithMany(x => x.Doser)
         .HasForeignKey(d => d.BehandlingId)
         .OnDelete(DeleteBehavior.Cascade);

        // Doseloggen beholdes nar brukeren slettes, med null der navnet sto.
        // Samme prinsipp som de andre *_av_bruker_id.
        b.HasOne(d => d.GittAv)
         .WithMany()
         .HasForeignKey(d => d.GittAvBrukerId)
         .OnDelete(DeleteBehavior.SetNull);

        // "Hvor mange i dag" er sporsmalet hver eneste gang.
        b.HasIndex(d => new { d.BehandlingId, d.GittTid })
         .IsDescending(false, true)
         .HasDatabaseName("ix_behandling_dose_behandling_tid");
    }
}
