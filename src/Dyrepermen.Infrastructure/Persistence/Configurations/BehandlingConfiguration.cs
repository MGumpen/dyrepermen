using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class BehandlingConfiguration : IEntityTypeConfiguration<Behandling>
{
    public void Configure(EntityTypeBuilder<Behandling> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityAlwaysColumn();

        b.Property(x => x.Preparat).HasMaxLength(80);
        b.Property(x => x.Styrke).HasMaxLength(40);
        b.Property(x => x.Notat).HasMaxLength(500);

        // Nostede betingelser, ikke switch. C# tillater ikke switch-uttrykk
        // i uttrykkstrer, og HasConversion krever nettopp det.
        b.Property(x => x.Type)
         .HasConversion(
             v => v == BehandlingType.Vaksine ? 'V'
                : v == BehandlingType.Ormekur ? 'O'
                : v == BehandlingType.Flatt ? 'F'
                : v == BehandlingType.Kloklipp ? 'K'
                : v == BehandlingType.Tannrens ? 'T'
                : 'A',
             v => v == 'V' ? BehandlingType.Vaksine
                : v == 'O' ? BehandlingType.Ormekur
                : v == 'F' ? BehandlingType.Flatt
                : v == 'K' ? BehandlingType.Kloklipp
                : v == 'T' ? BehandlingType.Tannrens
                : BehandlingType.Annet)
         .HasColumnType("char(1)")
         .IsRequired();

        b.HasOne(x => x.Dyr)
         .WithMany(d => d.Behandlinger)
         .HasForeignKey(x => x.DyrId)
         .OnDelete(DeleteBehavior.Cascade);

        // Partiell indeks - paminnelsesjobben treffer kun rader med neste dato.
        b.HasIndex(x => x.NesteDato)
         .HasFilter("neste_dato IS NOT NULL")
         .HasDatabaseName("ix_behandling_neste");

        // Standarden i databasen er for rader som ikke kommer fra appen.
        // Domenet setter 1 selv, sa EF sender alltid verdien - se
        // Behandling.AntallDager.
        b.Property(x => x.AntallDager).HasDefaultValue(1);
        b.Property(x => x.GangerPerDag).HasDefaultValue(1);

        b.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_behandling_type", "type IN ('V','O','F','K','T','A')");
            t.HasCheckConstraint(
                "ck_behandling_antall_dager", "antall_dager >= 1");
            t.HasCheckConstraint(
                "ck_behandling_ganger_per_dag", "ganger_per_dag >= 1");
        });
    }
}
