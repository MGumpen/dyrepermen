using Dyrepermen.Application.Services;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Ekte PostgreSQL i container. Ingen mocking av DbContext, og aldri
/// EF Core InMemory - den handhever verken constraints, partielle unike
/// indekser eller char(1)-konvertering, og gir gronne tester pa kode som
/// feiler i produksjon. Se plan kapittel 17.1.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("dyrepermen_test")
            .Build();

    public string Tilkobling => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // At migrasjonene kjores her er selv en test: feiler en migrasjon,
        // feiler hele suiten umiddelbart i stedet for ved neste utrulling.
        await using var db = LagContext(husstandId: 0);
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// Husstanden settes direkte pa holderen. Det er samme type som brukes i
    /// produksjon - middlewaren fyller den der, testen her. Ingen testdobbel.
    /// </summary>
    public DyrepermenDbContext LagContext(int husstandId)
    {
        var opt = new DbContextOptionsBuilder<DyrepermenDbContext>()
            .UseNpgsql(Tilkobling)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new DyrepermenDbContext(
            opt, new Husstandskontekst { HusstandId = husstandId });
    }

    /// <summary>
    /// Oppretter en husstand og returnerer den genererte ID-en.
    ///
    /// ID-ene kan ikke velges av testen: husstand.id er GENERATED ALWAYS AS
    /// IDENTITY, og eksplisitte verdier avvises av databasen. Hver test far
    /// derfor ferske ID-er, og er dermed uavhengig av rekkefolge og av andre
    /// testers data - slik plan kapittel 17.4 krever.
    /// </summary>
    public async Task<int> OpprettHusstand(string navn)
    {
        await using var db = LagContext(husstandId: 0);
        var husstand = new Husstand { Navn = navn };
        db.Husstand.Add(husstand);
        await db.SaveChangesAsync();
        return husstand.Id;
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

/// <summary>
/// Deler én container mellom testklassene. Isolasjonen kommer fra at hver
/// test bruker sine egne, genererte husstands-ID-er.
/// </summary>
[CollectionDefinition(Databasesamling.Navn)]
public sealed class Databasesamling : ICollectionFixture<DatabaseFixture>
{
    public const string Navn = "database";
}
