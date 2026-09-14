using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dyrepermen.Infrastructure.Persistence.Configurations;

public sealed class HusstandInnstillingConfiguration
    : IEntityTypeConfiguration<HusstandInnstilling>
{
    public void Configure(EntityTypeBuilder<HusstandInnstilling> b)
    {
        // Bade primaernokkel og fremmednokkel - skal ikke genereres.
        b.HasKey(i => i.HusstandId);
        b.Property(i => i.HusstandId).ValueGeneratedNever();

        b.Property(i => i.ForingsloggStandard).HasDefaultValue(false);
        b.Property(i => i.ForplanStandard).HasDefaultValue(true);
        // Ingen av bryterne har HasDefaultValue, og det er med vilje.
        //
        // For en bool er CLR-standarden false, og nar en egenskap har
        // lagringsstandard bruker EF nettopp CLR-standarden som sentinel:
        // verdien utelates fra INSERT, og databasens true slar inn. En bryter
        // som slas AV i samme kall som raden opprettes, ville altsa blitt
        // lagret som PA.
        //
        // VarslerAktiv hadde denne feilen. LagreInnstillinger oppretter raden
        // hvis den mangler (se HusstandService), og skrur man av varsler i
        // det oyeblikket, kom de tilbake ved neste sidelast. Samme familie
        // som HusstandInvitasjon.Rolle.
        //
        // Standardverdien ligger i stedet pa egenskapen i Domain, der den
        // gjelder uansett hvem som setter den. Migrasjonen fyller radene som
        // allerede finnes.
        b.Property(i => i.VarslerAktiv).IsRequired();
        b.Property(i => i.GodbitloggAktiv).IsRequired();

        b.HasOne(i => i.Husstand)
         .WithOne(h => h.Innstilling)
         .HasForeignKey<HusstandInnstilling>(i => i.HusstandId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
