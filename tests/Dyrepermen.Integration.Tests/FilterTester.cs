using Dyrepermen.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Filterproven fra plan kapittel 17.3. Fanger entiteter som er lagt til uten
/// query-filter - hullet som ellers oppdages forst nar noen ser andres data.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class FilterTester
{
    private readonly DatabaseFixture _fixture;

    public FilterTester(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public void Alle_husstandsbundne_entiteter_har_query_filter()
    {
        using var db = _fixture.LagContext(husstandId: 1);

        var utenFilter = db.Model.GetEntityTypes()
            .Where(t => t.ClrType.IsAssignableTo(typeof(IHusstandsbundet)))
            .Where(t => t.GetQueryFilter() is null)
            .Select(t => t.ClrType.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.True(
            utenFilter.Count == 0,
            $"Mangler query filter: {string.Join(", ", utenFilter)}");
    }

    [Fact]
    public void Alle_husstandsbundne_typer_er_med_i_modellen()
    {
        // Filterproven over ser kun entiteter som ER i modellen. En ny entitet
        // som implementerer IHusstandsbundet, men aldri blir kartlagt, er
        // usynlig for den. Denne testen dekker det hullet ved a sammenligne
        // domeneassemblyet mot modellen.
        var iDomenet = typeof(IHusstandsbundet).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.IsAssignableTo(typeof(IHusstandsbundet)))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToList();

        using var db = _fixture.LagContext(husstandId: 1);

        var iModellen = db.Model.GetEntityTypes()
            .Where(t => t.ClrType.IsAssignableTo(typeof(IHusstandsbundet)))
            .Select(t => t.ClrType.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(iDomenet, iModellen);
    }
}
