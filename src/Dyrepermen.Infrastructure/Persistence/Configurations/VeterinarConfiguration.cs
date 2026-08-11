using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class VeterinarConfiguration : IEntityTypeConfiguration<Veterinar>
{
    public void Configure(EntityTypeBuilder<Veterinar> b)
    {
        b.HasKey(v => v.Id);
        b.Property(v => v.Id).UseIdentityAlwaysColumn();

        b.Property(v => v.Navn).HasMaxLength(100).IsRequired();

        // varchar, ikke char. char(20) blank-padder i PostgreSQL, og da er
        // verdien ikke lenger lik seg selv ved sammenligning.
        b.Property(v => v.Telefon).HasMaxLength(20);
        b.Property(v => v.Adresse).HasMaxLength(200);
        b.Property(v => v.Nettside).HasMaxLength(200);
        b.Property(v => v.Epost).HasMaxLength(200);
        b.Property(v => v.Apningstider).HasMaxLength(200);
        b.Property(v => v.Notat).HasMaxLength(500);

        b.Property(v => v.OpprettetDato)
         .HasDefaultValueSql("CURRENT_DATE")
         .IsRequired();

        // Ingen HasDefaultValue: Fast er CLR-standarden, og med en
        // lagringsstandard ville EF utelatt kolonnen nettopp for den verdien.
        // Samme felle som HusstandInvitasjon.Rolle.
        // Nostede betingelser, ikke switch. C# tillater ikke switch-uttrykk
        // i uttrykkstrer, og HasConversion krever nettopp det.
        b.Property(v => v.Type)
         .HasConversion(
             v => v == Veterinartype.Vakt ? 'V'
                : v == Veterinartype.Sykehus ? 'S'
                : v == Veterinartype.Annet ? 'A'
                : 'F',
             v => v == 'V' ? Veterinartype.Vakt
                : v == 'S' ? Veterinartype.Sykehus
                : v == 'A' ? Veterinartype.Annet
                : Veterinartype.Fast)
         .HasColumnType("char(1)")
         .IsRequired();

        b.HasOne(v => v.Husstand)
         .WithMany()
         .HasForeignKey(v => v.HusstandId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(v => new { v.HusstandId, v.Navn })
         .HasDatabaseName("ix_veterinar_husstand_navn");

        b.ToTable(t =>
        {
            t.HasCheckConstraint("ck_veterinar_type", "type IN ('F','V','S','A')");

            // Lengdevilkar i stedet for formatvalidering. Utenlandske numre,
            // kortnumre og numre med landkode skal alle ga gjennom.
            t.HasCheckConstraint(
                "ck_veterinar_telefon",
                "telefon IS NULL OR length(btrim(telefon)) BETWEEN 3 AND 20");
        });
    }
}
