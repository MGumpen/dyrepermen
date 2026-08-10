using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class ForplanConfiguration : IEntityTypeConfiguration<Forplan>
{
    public void Configure(EntityTypeBuilder<Forplan> b)
    {
        b.HasKey(f => f.Id);
        b.Property(f => f.Id).UseIdentityAlwaysColumn();

        b.Property(f => f.Fornavn).HasMaxLength(80);
        b.Property(f => f.Notat).HasMaxLength(300);
        b.Property(f => f.AntallMaltider).HasDefaultValue(2);
        b.Property(f => f.Aktiv).HasDefaultValue(true);

        b.Property(f => f.OpprettetDato)
         .HasDefaultValueSql("CURRENT_DATE")
         .IsRequired();

        b.Property(f => f.Metode)
         .HasConversion(
             v => v == Formetode.Prosent ? 'P' : 'G',
             v => v == 'P' ? Formetode.Prosent : Formetode.Gram)
         .HasColumnType("char(1)")
         .IsRequired();

        b.Property<uint>("Xmin")
         .HasColumnName("xmin")
         .HasColumnType("xid")
         .ValueGeneratedOnAddOrUpdate()
         .IsConcurrencyToken();

        b.HasOne(f => f.Dyr)
         .WithMany(d => d.Forplaner)
         .HasForeignKey(f => f.DyrId)
         .OnDelete(DeleteBehavior.Cascade);

        // Kun en aktiv plan per dyr.
        b.HasIndex(f => f.DyrId)
         .IsUnique()
         .HasFilter("aktiv")
         .HasDatabaseName("ux_forplan_aktiv");

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_forplan_metode", "metode IN ('P','G')");

            // Gjor de to metodene gjensidig utelukkende. Databasen skal ikke
            // kunne inneholde en plan som er halvt prosentbasert og halvt fast.
            t.HasCheckConstraint("ck_forplan_verdi", """
                (metode = 'P' AND prosent_tidels IS NOT NULL
                              AND prosent_tidels BETWEEN 1 AND 300
                              AND gram_per_dag IS NULL)
             OR (metode = 'G' AND gram_per_dag IS NOT NULL
                              AND gram_per_dag > 0
                              AND prosent_tidels IS NULL)
             """);

            t.HasCheckConstraint(
                "ck_forplan_maltider", "antall_maltider BETWEEN 1 AND 6");
        });
    }
}
