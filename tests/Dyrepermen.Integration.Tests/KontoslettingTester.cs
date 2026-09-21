using Dyrepermen.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Kontosletting over HTTP, gjennom den felles slettemetoden som ogsa
/// rydder demoer. Se ADR 0015 avsnitt 6.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class KontoslettingTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public KontoslettingTester(DatabaseFixture fixture) => _fixture = fixture;

    public Task InitializeAsync()
    {
        _app = new Appfabrikk(_fixture.Tilkobling);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _app.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Konto_med_handlelistevare_pa_et_dyr_kan_slettes()
    {
        // Handlelisten peker pa dyret med RESTRICT. Ble dyrene slettet for
        // handlelisten, stoppet fremmednokkelen hele slettingen.
        var epost = $"slett-{Guid.NewGuid():N}@example.test";
        var klient = await Testoppsett.InnloggetKlient(_app, epost);
        var dyrId = await Testoppsett.NyttDyr(klient);

        int husstandId;
        await using (var db = _fixture.LagContext(husstandId: 0))
        {
            husstandId = await db.Dyr.IgnoreQueryFilters()
                .Where(d => d.Id == dyrId)
                .Select(d => d.HusstandId)
                .SingleAsync();

            db.Handleliste.Add(new Handleliste
            {
                HusstandId = husstandId,
                DyrId = dyrId,
                Tekst = "Tørrfôr",
                OpprettetDato = DateOnly.FromDateTime(DateTime.Today)
            });
            await db.SaveChangesAsync();
        }

        var svar = await klient.Post("/konto/slett", new Dictionary<string, string>
        {
            ["Slett.Passord"] = "Passord123",
            ["Slett.Bekreftelse"] = "SLETT",
            ["Slett.BekrefterHusstandsletting"] = "true"
        }, tokenFra: "/konto");

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Kontoen ble ikke slettet: {await Skjemaklient.Feilmeldinger(svar)}");

        await using var etter = _fixture.LagContext(husstandId: 0);

        Assert.False(await etter.Users.AnyAsync(
            u => u.NormalizedEmail == epost.ToUpperInvariant()));
        Assert.False(await etter.Husstand.AnyAsync(h => h.Id == husstandId));
    }
}
