using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class DyrConfiguration : IEntityTypeConfiguration<Dyr>
{
    public void Configure(EntityTypeBuilder<Dyr> b)
    {
        b.HasKey(d => d.Id);
        b.Property(d => d.Id).UseIdentityAlwaysColumn();

        b.Property(d => d.Navn).HasMaxLength(60).IsRequired();
        b.Property(d => d.Rase).HasMaxLength(80);
        b.Property(d => d.BildeFilnavn).HasMaxLength(120);

        // VARCHAR(15) med lengde-CHECK, ikke CHAR(15). PostgreSQL blank-padder
        // char(n), slik at verdien kommer tilbake med etterfolgende mellomrom
        // og odelegger bade sammenligning og unikhetssjekk. Plan kapittel 5.2.
        b.Property(d => d.ChipNr).HasMaxLength(15);
        b.Property(d => d.RegNrNkk).HasMaxLength(20);

        // Enum til char(1). Ma mappes eksplisitt - gjettes verdiene, brytes
        // CHECK-constrainten ved forste lagring. Plan kapittel 8.1.
        b.Property(d => d.Art)
         .HasConversion(
             v => v == Art.Hund ? 'H' : 'K',
             v => v == 'H' ? Art.Hund : Art.Katt)
         .HasColumnType("char(1)")
         .IsRequired();

        b.Property(d => d.Kjonn)
         .HasConversion(
             v => v == Kjonn.Tispe ? 'T' : 'H',
             v => v == 'T' ? Kjonn.Tispe : Kjonn.Hann)
         .HasColumnType("char(1)")
         .IsRequired();

        b.Property(d => d.Kastrert).HasDefaultValue(false);
        b.Property(d => d.ForingsloggAktiv).HasDefaultValue(false);
        b.Property(d => d.ForplanAktiv).HasDefaultValue(true);
        b.Property(d => d.Aktiv).HasDefaultValue(true);

        // Samtidighetstoken pa Postgres' interne xmin-kolonne. To brukere som
        // redigerer samme rad samtidig gir ellers tapt oppdatering uten videre.
        // Plan kapittel 5.4. UseXminAsConcurrencyToken er utfaset i Npgsql 9.
        b.Property<uint>("Xmin")
         .HasColumnName("xmin")
         .HasColumnType("xid")
         .ValueGeneratedOnAddOrUpdate()
         .IsConcurrencyToken();

        // Ingen cascade fra husstand - sletting skal aldri skje utilsiktet.
        b.HasOne(d => d.Husstand)
         .WithMany(h => h.Dyr)
         .HasForeignKey(d => d.HusstandId)
         .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(d => d.HusstandId).HasDatabaseName("ix_dyr_husstand");

        // Unikhet er global, ikke per husstand. Chipnummer er unike pa
        // verdensbasis. Feilmeldingen ma derfor vaere noytral og ikke avslore
        // hvilken husstand dyret tilhorer. Plan kapittel 5.3.
        b.HasIndex(d => d.ChipNr)
         .IsUnique()
         .HasFilter("chip_nr IS NOT NULL")
         .HasDatabaseName("ux_dyr_chip");

        // ux_dyr_regnr er funksjonell - upper(reg_nr_nkk) - og legges inn som
        // ra SQL i migrasjonen.

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_dyr_art", "art IN ('H','K')");
            t.HasCheckConstraint("ck_dyr_kjonn", "kjonn IN ('T','H')");
            t.HasCheckConstraint(
                "ck_dyr_chip_lengde",
                "chip_nr IS NULL OR char_length(chip_nr) = 15");
        });
    }
}
