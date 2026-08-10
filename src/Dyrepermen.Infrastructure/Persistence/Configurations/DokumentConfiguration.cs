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

        b.Property(x => x.Filnavn).HasMaxLength(200).IsRequired();
        b.Property(x => x.Originalnavn).HasMaxLength(200).IsRequired();

        b.Property(x => x.OpplastetDato)
         .HasDefaultValueSql("CURRENT_DATE")
         .IsRequired();

        b.Property(x => x.Kategori)
         .HasConversion(
             v => v == DokumentKategori.Vaksinebok ? 'V'
                : v == DokumentKategori.Journal ? 'J'
                : v == DokumentKategori.Kvittering ? 'K'
                : 'A',
             v => v == 'V' ? DokumentKategori.Vaksinebok
                : v == 'J' ? DokumentKategori.Journal
                : v == 'K' ? DokumentKategori.Kvittering
                : DokumentKategori.Annet)
         .HasColumnType("char(1)")
         .IsRequired();

        b.HasOne(x => x.Dyr)
         .WithMany(d => d.Dokumenter)
         .HasForeignKey(x => x.DyrId)
         .OnDelete(DeleteBehavior.Cascade);

        b.ToTable(t => t.HasCheckConstraint(
            "ck_dokument_kategori", "kategori IN ('V','J','K','A')"));
    }
}
