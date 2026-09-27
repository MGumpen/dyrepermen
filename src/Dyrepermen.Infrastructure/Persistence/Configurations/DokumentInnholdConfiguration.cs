using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class DokumentInnholdConfiguration : IEntityTypeConfiguration<DokumentInnhold>
{
    public void Configure(EntityTypeBuilder<DokumentInnhold> b)
    {
        // Primaernokkelen er ogsa fremmednokkelen: ett innhold per dokument.
        b.HasKey(x => x.DokumentId);
        b.Property(x => x.DokumentId).ValueGeneratedNever();

        b.Property(x => x.Data).HasColumnType("bytea").IsRequired();
    }
}
