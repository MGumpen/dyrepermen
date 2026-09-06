using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class ForplantrinnConfiguration : IEntityTypeConfiguration<Forplantrinn>
{
    public void Configure(EntityTypeBuilder<Forplantrinn> b)
    {
        b.HasKey(t => t.Id);
        b.Property(t => t.Id).UseIdentityAlwaysColumn();

        b.HasOne(t => t.Forplan)
         .WithMany(f => f.Tabelltrinn)
         .HasForeignKey(t => t.ForplanId)
         .OnDelete(DeleteBehavior.Cascade);

        // To rader pa samme maned er ikke en plan, det er et sporsmal uten
        // svar. Interpolasjonen ville matt gjette hvilken av dem som gjelder.
        b.HasIndex(t => new { t.ForplanId, t.AlderMnd })
         .IsUnique()
         .HasDatabaseName("ux_forplantrinn_alder");

        b.ToTable(t =>
        {
            // 240 maneder er tjue ar. Ingen hund eller katt kommer dit, men
            // grensen holder en skrivefeil pa fire siffer ute av tabellen.
            t.HasCheckConstraint("ck_forplantrinn_alder", "alder_mnd BETWEEN 0 AND 240");

            t.HasCheckConstraint(
                "ck_forplantrinn_gram", "gram_per_dag BETWEEN 1 AND 20000");
        });
    }
}
