using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Infrastructure.Persistence;

/// <summary>
/// Implementerer IDataProtectionKeyContext slik at nokkelringen lagres i
/// databasen. Ligger nokklene pa filsystemet i containeren, forsvinner de ved
/// hver utrulling og alle blir logget ut. Tabellen data_protection_keys ma
/// aldri tommes. Se plan kapittel 11.2.
/// </summary>
public sealed class DyrepermenDbContext
    : IdentityDbContext<Bruker, IdentityRole<int>, int>, IDataProtectionKeyContext
{
    private readonly IHusstandContext _husstand;

    public DyrepermenDbContext(
        DbContextOptions<DyrepermenDbContext> options,
        IHusstandContext husstand) : base(options)
    {
        _husstand = husstand;
    }

    public DbSet<Husstand> Husstand => Set<Husstand>();
    public DbSet<HusstandInnstilling> HusstandInnstilling => Set<HusstandInnstilling>();
    public DbSet<HusstandInvitasjon> HusstandInvitasjon => Set<HusstandInvitasjon>();
    public DbSet<Dyr> Dyr => Set<Dyr>();
    public DbSet<Vekt> Vekt => Set<Vekt>();
    public DbSet<Behandling> Behandling => Set<Behandling>();
    public DbSet<Medisin> Medisin => Set<Medisin>();
    public DbSet<Dose> Dose => Set<Dose>();
    public DbSet<Forplan> Forplan => Set<Forplan>();
    public DbSet<Foring> Foring => Set<Foring>();
    public DbSet<Vetbesok> Vetbesok => Set<Vetbesok>();
    public DbSet<Forsikring> Forsikring => Set<Forsikring>();
    public DbSet<Dokument> Dokument => Set<Dokument>();
    public DbSet<Handleliste> Handleliste => Set<Handleliste>();
    public DbSet<Informasjon> Informasjon => Set<Informasjon>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.ApplyConfigurationsFromAssembly(typeof(DyrepermenDbContext).Assembly);
        b.BrukSnakeCase();

        // ------------------------------------------------------------------
        // Globale query-filtre.
        //
        // VEDLIKEHOLDSREGEL: hver nye entitet som implementerer
        // IHusstandsbundet skal ha et filter her. Listen under ma ha like
        // mange oppforinger som det finnes husstandsbundne entiteter.
        //
        // Filterproven i Dyrepermen.Integration.Tests teller dem og feiler
        // hvis en mangler. Men proven ser kun det som er markert med
        // IHusstandsbundet - glemmes bade grensesnittet og filteret, er
        // entiteten synlig for alle husstander uten at noe sier fra.
        // ------------------------------------------------------------------

        b.Entity<Dyr>()
         .HasQueryFilter(d => d.HusstandId == _husstand.HusstandId && d.Aktiv);

        b.Entity<Handleliste>()
         .HasQueryFilter(h => h.HusstandId == _husstand.HusstandId);

        b.Entity<Informasjon>()
         .HasQueryFilter(i => i.HusstandId == _husstand.HusstandId);

        b.Entity<HusstandInnstilling>()
         .HasQueryFilter(i => i.HusstandId == _husstand.HusstandId);

        b.Entity<HusstandInvitasjon>()
         .HasQueryFilter(i => i.HusstandId == _husstand.HusstandId);

        b.Entity<Vekt>()
         .HasQueryFilter(v => v.Dyr.HusstandId == _husstand.HusstandId);

        b.Entity<Behandling>()
         .HasQueryFilter(x => x.Dyr.HusstandId == _husstand.HusstandId);

        b.Entity<Medisin>()
         .HasQueryFilter(m => m.Dyr.HusstandId == _husstand.HusstandId);

        b.Entity<Dose>()
         .HasQueryFilter(d => d.Medisin.Dyr.HusstandId == _husstand.HusstandId);

        b.Entity<Forplan>()
         .HasQueryFilter(f => f.Dyr.HusstandId == _husstand.HusstandId);

        b.Entity<Foring>()
         .HasQueryFilter(f => f.Dyr.HusstandId == _husstand.HusstandId);

        b.Entity<Vetbesok>()
         .HasQueryFilter(x => x.Dyr.HusstandId == _husstand.HusstandId);

        b.Entity<Forsikring>()
         .HasQueryFilter(f => f.Dyr.HusstandId == _husstand.HusstandId);

        b.Entity<Dokument>()
         .HasQueryFilter(x => x.Dyr.HusstandId == _husstand.HusstandId);
    }
}
