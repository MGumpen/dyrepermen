using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class InformasjonConfiguration : IEntityTypeConfiguration<Informasjon>
{
    public void Configure(EntityTypeBuilder<Informasjon> b)
    {
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).UseIdentityAlwaysColumn();

        b.Property(i => i.Tittel).HasMaxLength(80).IsRequired();
        b.Property(i => i.Tekst).HasMaxLength(2000).IsRequired();

        b.Property(i => i.OpprettetDato)
         .HasDefaultValueSql("CURRENT_DATE")
         .IsRequired();

        // Ingen cascade fra husstand, som for de ovrige husstandsbundne
        // tabellene. Sletting av en husstand skal aldri skje utilsiktet.
        b.HasOne(i => i.Husstand)
         .WithMany()
         .HasForeignKey(i => i.HusstandId)
         .OnDelete(DeleteBehavior.Restrict);

        // Slettes dyret, forsvinner notatene om det. De handler om dyret.
        b.HasOne(i => i.Dyr)
         .WithMany()
         .HasForeignKey(i => i.DyrId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(i => i.OpprettetAv)
         .WithMany()
         .HasForeignKey(i => i.OpprettetAvBrukerId)
         .OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(i => i.HusstandId).HasDatabaseName("ix_informasjon_husstand");
    }
}
