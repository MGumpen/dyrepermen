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
        b.Property(f => f.FornavnAlder).HasMaxLength(80);
        b.Property(f => f.Notat).HasMaxLength(300);
        b.Property(f => f.AntallMaltider).HasDefaultValue(2);
        b.Property(f => f.Aktiv).HasDefaultValue(true);

        b.Property(f => f.OpprettetDato)
         .HasDefaultValueSql("CURRENT_DATE")
         .IsRequired();

        // Eksplisitt mapping begge veier. Gjetter man pa verdiene, brytes
        // ck_forplan_metode ved forste lagring.
        // Nostede betingelser, ikke switch: HasConversion tar uttrykkstraer,
        // og en switch-uttrykk kompilerer ikke inn i et slikt.
        b.Property(f => f.Metode)
         .HasConversion(
             v => v == Formetode.Prosent ? 'P' : v == Formetode.Gram ? 'G' : 'T',
             v => v == 'P' ? Formetode.Prosent
                : v == 'G' ? Formetode.Gram
                : Formetode.Tabell)
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
            t.HasCheckConstraint("ck_forplan_metode", "metode IN ('P','G','T')");

            // Gjor de tre metodene gjensidig utelukkende. Databasen skal ikke
            // kunne inneholde en plan som er halvt prosentbasert og halvt fast.
            //
            // Tabellmetoden bruker vektdel_andel_prosent som blandingsforhold.
            // Star den pa 0, er planen ren tabell, og da finnes det ingen
            // raforregel a kreve - prosent_tidels skal vaere null. Ellers ma
            // den vaere satt, ellers ville andelen skalert ingenting.
            //
            // Andelen skal vaere null pa de to enkle metodene - ellers blir
            // det liggende igjen et tall som ser ut som en regel, men ikke
            // brukes til noe.
            t.HasCheckConstraint("ck_forplan_verdi", """
                (metode = 'P' AND prosent_tidels IS NOT NULL
                              AND prosent_tidels BETWEEN 1 AND 300
                              AND gram_per_dag IS NULL
                              AND vektdel_andel_prosent IS NULL)
             OR (metode = 'G' AND gram_per_dag IS NOT NULL
                              AND gram_per_dag > 0
                              AND prosent_tidels IS NULL
                              AND vektdel_andel_prosent IS NULL)
             OR (metode = 'T' AND gram_per_dag IS NULL
                              AND vektdel_andel_prosent IS NOT NULL
                              AND vektdel_andel_prosent BETWEEN 0 AND 100
                              AND ((vektdel_andel_prosent = 0
                                    AND prosent_tidels IS NULL)
                                OR (vektdel_andel_prosent > 0
                                    AND prosent_tidels IS NOT NULL
                                    AND prosent_tidels BETWEEN 1 AND 300)))
             """);

            t.HasCheckConstraint(
                "ck_forplan_maltider", "antall_maltider BETWEEN 1 AND 6");
        });
    }
}
