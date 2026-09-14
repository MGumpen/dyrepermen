using Dyrepermen.Infrastructure.Persistence;
using Npgsql;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Beviset som mangler over: at Npgsql faktisk GODTAR det normaliseringen
/// lager. En strengtest kan gi riktig form og likevel noe serveren avviser -
/// og det var nettopp dette som stoppet utrullingen.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class TilkoblingsstrengMotEkteServer
{
    private readonly DatabaseFixture _fixture;

    public TilkoblingsstrengMotEkteServer(DatabaseFixture fixture)
        => _fixture = fixture;

    [Fact]
    public async Task URI_form_kan_faktisk_kobles_til()
    {
        var kilde = new NpgsqlConnectionStringBuilder(_fixture.Tilkobling);

        // Samme form som Neon oppgir. sslmode=disable fordi testcontaineren
        // ikke har TLS - normaliseringen krever ellers Require, og det er
        // med vilje: en database pa internett skal ikke kunne nas i klartekst.
        var uri = $"postgresql://{kilde.Username}:{kilde.Password}"
                + $"@{kilde.Host}:{kilde.Port}/{kilde.Database}?sslmode=disable";

        await using var tilkobling = new NpgsqlConnection(
            Tilkoblingsstreng.Normaliser(uri));

        await tilkobling.OpenAsync();

        await using var kommando = new NpgsqlCommand("SELECT 1", tilkobling);

        Assert.Equal(1, await kommando.ExecuteScalarAsync());
    }
}
