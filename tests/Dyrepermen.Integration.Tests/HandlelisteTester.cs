using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Services;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Dyrepermen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Integration.Tests;

/// <summary>Fase 6, plan kapittel 16.</summary>
[Collection(Databasesamling.Navn)]
public sealed class HandlelisteTester
{
    private readonly DatabaseFixture _fixture;

    public HandlelisteTester(DatabaseFixture fixture) => _fixture = fixture;

    private static HandlelisteService Tjeneste(DyrepermenDbContext db, int husstand)
        => new(db, new Husstandskontekst { HusstandId = husstand });

    private static async Task<int> NyttDyr(DyrepermenDbContext db, int husstand)
    {
        var dyr = new Dyr
        {
            HusstandId = husstand,
            Navn = "Luna",
            Art = Art.Hund,
            Kjonn = Kjonn.Tispe
        };
        db.Dyr.Add(dyr);
        await db.SaveChangesAsync();
        return dyr.Id;
    }

    [Fact]
    public async Task Punkt_uten_dyr_har_ingen_dyrenavn_og_vises_som_felles()
    {
        var h = await _fixture.OpprettHusstand("Felles");
        await using var db = _fixture.LagContext(h);
        var tjeneste = Tjeneste(db, h);

        var dyrId = await NyttDyr(db, h);

        await tjeneste.Legg(new NyttPunkt("Tørrfôr", 1, null, null), default);
        await tjeneste.Legg(new NyttPunkt("Ormekur", 2, dyrId, null), default);

        var punkter = await tjeneste.Hent(default);

        // Null dyrenavn er det visningen gjor om til "Felles".
        Assert.Null(punkter.Single(p => p.Tekst == "Tørrfôr").DyreNavn);
        Assert.Equal("Luna", punkter.Single(p => p.Tekst == "Ormekur").DyreNavn);
    }

    [Fact]
    public async Task Avkryssing_veksler_status_begge_veier()
    {
        var h = await _fixture.OpprettHusstand("Kryss");
        await using var db = _fixture.LagContext(h);
        var tjeneste = Tjeneste(db, h);

        await tjeneste.Legg(new NyttPunkt("Kattemat", 1, null, null), default);
        var id = (await tjeneste.Hent(default)).Single().Id;

        await tjeneste.VekslStatus(id, default);
        Assert.Equal(HandlelisteStatus.Kjopt,
            (await tjeneste.Hent(default)).Single().Status);

        // Feilklikk skal kunne angres.
        await tjeneste.VekslStatus(id, default);
        Assert.Equal(HandlelisteStatus.Aktiv,
            (await tjeneste.Hent(default)).Single().Status);
    }

    [Fact]
    public async Task Aktive_punkter_kommer_forst_i_listen()
    {
        var h = await _fixture.OpprettHusstand("Rekkefolge");
        await using var db = _fixture.LagContext(h);
        var tjeneste = Tjeneste(db, h);

        await tjeneste.Legg(new NyttPunkt("Forst", 1, null, null), default);
        await tjeneste.Legg(new NyttPunkt("Andre", 1, null, null), default);

        var forste = (await tjeneste.Hent(default)).First().Id;
        await tjeneste.VekslStatus(forste, default);

        var punkter = await tjeneste.Hent(default);
        Assert.Equal("Andre", punkter[0].Tekst);
        Assert.Equal("Forst", punkter[1].Tekst);
    }

    [Fact]
    public async Task Dashbordet_viser_kun_de_fem_oeverste_aktive()
    {
        var h = await _fixture.OpprettHusstand("Topp fem");
        await using var db = _fixture.LagContext(h);
        var tjeneste = Tjeneste(db, h);

        for (var i = 1; i <= 7; i++)
        {
            await tjeneste.Legg(new NyttPunkt($"Vare {i}", 1, null, null), default);
        }

        var kjopt = (await tjeneste.Hent(default)).First().Id;
        await tjeneste.VekslStatus(kjopt, default);

        var topp = await tjeneste.HentAktive(5, default);

        Assert.Equal(5, topp.Count);
        Assert.All(topp, p => Assert.Equal(HandlelisteStatus.Aktiv, p.Status));
        Assert.DoesNotContain(topp, p => p.Tekst == "Vare 1");
    }

    [Fact]
    public async Task Husstand_ser_ikke_annen_husstands_handleliste()
    {
        var a = await _fixture.OpprettHusstand("Liste A");
        var b = await _fixture.OpprettHusstand("Liste B");

        await using var db = _fixture.LagContext(a);
        await Tjeneste(db, a).Legg(new NyttPunkt("Hemmelig", 1, null, null), default);

        await using var annen = _fixture.LagContext(b);
        Assert.Empty(await Tjeneste(annen, b).Hent(default));
    }

    [Fact]
    public async Task Punkt_kan_ikke_kobles_til_annen_husstands_dyr()
    {
        var a = await _fixture.OpprettHusstand("Eier liste");
        var b = await _fixture.OpprettHusstand("Fremmed liste");

        int dyrId;
        await using (var eier = _fixture.LagContext(a))
        {
            dyrId = await NyttDyr(eier, a);
        }

        await using var fremmed = _fixture.LagContext(b);

        Assert.False(await Tjeneste(fremmed, b)
            .Legg(new NyttPunkt("Forsok", 1, dyrId, null), default));
    }

    [Fact]
    public async Task Rydd_fjerner_kun_kjopte()
    {
        var h = await _fixture.OpprettHusstand("Rydd");
        await using var db = _fixture.LagContext(h);
        var tjeneste = Tjeneste(db, h);

        await tjeneste.Legg(new NyttPunkt("Beholdes", 1, null, null), default);
        await tjeneste.Legg(new NyttPunkt("Fjernes", 1, null, null), default);

        var fjernes = (await tjeneste.Hent(default)).Single(p => p.Tekst == "Fjernes").Id;
        await tjeneste.VekslStatus(fjernes, default);

        Assert.Equal(1, await tjeneste.RyddKjopte(default));

        var igjen = await tjeneste.Hent(default);
        Assert.Equal("Beholdes", igjen.Single().Tekst);
    }

    [Fact]
    public async Task Tom_tekst_avvises()
    {
        var h = await _fixture.OpprettHusstand("Tom tekst");
        await using var db = _fixture.LagContext(h);

        Assert.False(await Tjeneste(db, h)
            .Legg(new NyttPunkt("   ", 1, null, null), default));
    }
}
