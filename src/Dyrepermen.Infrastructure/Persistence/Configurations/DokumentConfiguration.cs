using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class DokumentConfiguration : IEntityTypeConfiguration<Dokument>
{
    public void Configure(EntityTypeBuilder<Dokument> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityAlwaysColumn();

        b.Property(x => x.Originalnavn).HasMaxLength(200).IsRequired();
        b.Property(x => x.Innholdstype).HasMaxLength(50).IsRequired();
        b.Property(x => x.StorrelseByte).IsRequired();

        b.Property(x => x.OpplastetDato)
         .HasDefaultValueSql("CURRENT_DATE")
         .IsRequired();

        b.Property(x => x.Kategori)
         .HasConversion(
             v => v == DokumentKategori.Vaksinebok ? 'V'
                : v == DokumentKategori.Journal ? 'J'
                : v == DokumentKategori.Kvittering ? 'K'
                : v == DokumentKategori.Profilbilde ? 'P'
                : 'A',
             v => v == 'V' ? DokumentKategori.Vaksinebok
                : v == 'J' ? DokumentKategori.Journal
                : v == 'K' ? DokumentKategori.Kvittering
                : v == 'P' ? DokumentKategori.Profilbilde
                : DokumentKategori.Annet)
         .HasColumnType("char(1)")
         .IsRequired();

        b.HasOne(x => x.Dyr)
         .WithMany(d => d.Dokumenter)
         .HasForeignKey(x => x.DyrId)
         .OnDelete(DeleteBehavior.Cascade);

        // Vedlegget horer til besoket. Slettes timen, gar kvitteringen med -
        // en kvittering uten besok har ingen a vise den fram for.
        // Hoyst ett profilbilde per dyr. Tjenesten bytter det gamle ut, men
        // det er indeksen som gjor at to samtidige opplastinger ikke gir to.
        //
        // Begge indeksene pa dyr_id er skrevet ut, med navn. Star bare den
        // filtrerte, regner EF den som fremmednokkelens indeks og lar den
        // vanlige forsvinne - og kaskaden fra dyret mister indeksen sin uten
        // at noe sier fra. Navnekonvensjonen ville dessuten gitt begge samme
        // navn, derfor HasDatabaseName.
        b.HasIndex(x => x.DyrId, "ix_dokument_dyr_id")
         .HasDatabaseName("ix_dokument_dyr_id");

        b.HasIndex(x => x.DyrId, "ux_dokument_profilbilde")
         .IsUnique()
         .HasFilter("kategori = 'P'")
         .HasDatabaseName("ux_dokument_profilbilde");

        b.HasOne(x => x.Vetbesok)
         .WithMany(v => v.Vedlegg)
         .HasForeignKey(x => x.VetbesokId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Innhold)
         .WithOne(i => i.Dokument)
         .HasForeignKey<DokumentInnhold>(i => i.DokumentId)
         .OnDelete(DeleteBehavior.Cascade);

        b.ToTable(t =>
        {
            t.HasCheckConstraint(
                "ck_dokument_kategori", "kategori IN ('V','J','K','A','P')");

            // Et profilbilde er et bilde. En PDF som profilbilde ville gitt
            // et knust bilde pa dashbordet.
            t.HasCheckConstraint(
                "ck_dokument_profilbilde_er_bilde",
                "kategori <> 'P' OR innholdstype IN ('image/jpeg','image/png')");

            // Hvitlisten fra plan kapittel 15. Controlleren og tjenesten
            // sjekker den ogsa, men databasen er siste skanse.
            t.HasCheckConstraint(
                "ck_dokument_innholdstype",
                "innholdstype IN ('image/jpeg','image/png','application/pdf')");

            t.HasCheckConstraint("ck_dokument_storrelse", "storrelse_byte > 0");
        });
    }
}
