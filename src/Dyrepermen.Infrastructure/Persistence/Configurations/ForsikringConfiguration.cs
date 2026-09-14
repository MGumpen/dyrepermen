using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class ForsikringConfiguration : IEntityTypeConfiguration<Forsikring>
{
    public void Configure(EntityTypeBuilder<Forsikring> b)
    {
        b.HasKey(f => f.Id);
        b.Property(f => f.Id).UseIdentityAlwaysColumn();

        b.Property(f => f.Selskap).HasMaxLength(80).IsRequired();
        b.Property(f => f.PoliseNr).HasMaxLength(40);

        b.Property(f => f.ArspremieKr).HasDefaultValue(0);
        b.Property(f => f.ForsikringsbelopKr).HasDefaultValue(0);
        b.Property(f => f.EgenandelFastKr).HasDefaultValue(0);
        b.Property(f => f.EgenandelVariabelTidels).HasDefaultValue(0);

        b.HasOne(f => f.Dyr)
         .WithMany(d => d.Forsikringer)
         .HasForeignKey(f => f.DyrId)
         .OnDelete(DeleteBehavior.Cascade);

        // Partiell indeks - paminnelsesjobben treffer kun rader med dato.
        b.HasIndex(f => f.FornyesDato)
         .HasFilter("fornyes_dato IS NOT NULL")
         .HasDatabaseName("ix_forsikring_fornyes");

        b.ToTable(t => t.HasCheckConstraint(
            "ck_forsikring_variabel",
            "egenandel_variabel_tidels BETWEEN 0 AND 1000"));
    }
}
