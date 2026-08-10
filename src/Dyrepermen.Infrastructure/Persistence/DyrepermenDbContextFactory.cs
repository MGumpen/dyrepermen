using Dyrepermen.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dyrepermen.Infrastructure.Persistence;

/// <summary>
/// Brukes kun av "dotnet ef" ved generering av migrasjoner. Verktoyet ma kunne
/// lage en DbContext uten a starte webverten, og DyrepermenDbContext krever
/// IHusstandContext i konstruktoren.
///
/// Husstanden settes til 0 her. Migrasjonsgenerering leser aldri data, og 0
/// gir uansett tomt resultatsett om noen skulle prove.
///
/// UseSnakeCaseNamingConvention MA vaere med. Utelates den, genererer
/// migrasjonen PascalCase-navn, og skjemaet stemmer ikke med kapittel 5.
/// </summary>
public sealed class DyrepermenDbContextFactory
    : IDesignTimeDbContextFactory<DyrepermenDbContext>
{
    // Lokal utvikling. Port 5434 - se ADR 0006 om hvorfor ikke 5432.
    // CI og produksjon setter ConnectionStrings__Postgres som miljovariabel.
    private const string Reserve =
        "Host=localhost;Port=5434;Database=dyrepermen;" +
        "Username=dyrepermen;Password=utvikling";

    public DyrepermenDbContext CreateDbContext(string[] args)
    {
        // Ingen tilkobling apnes ved generering, men Npgsql krever en
        // syntaktisk gyldig streng for a bygge modellen.
        var tilkobling =
            Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? Reserve;

        var opt = new DbContextOptionsBuilder<DyrepermenDbContext>()
            .UseNpgsql(tilkobling, npg =>
                npg.MigrationsAssembly("Dyrepermen.Infrastructure"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new DyrepermenDbContext(opt, new Husstandskontekst());
    }
}
