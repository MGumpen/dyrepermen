using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Verifiserer at databasen faktisk handhever reglene i plan kapittel 5.
/// Dette er grunnen til at InMemory-provideren ikke kan brukes - den kjenner
/// ingen av disse constraintene.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class SkjemaTester
{
    private readonly DatabaseFixture _fixture;

    public SkjemaTester(DatabaseFixture fixture) => _fixture = fixture;

    private static Dyr NyttDyr(int husstandId, string navn, string? chip = null)
        => new()
        {
            HusstandId = husstandId,
            Navn = navn,
            Art = Art.Hund,
            Kjonn = Kjonn.Tispe,
            ChipNr = chip
        };

    [Fact]
    public async Task Duplikat_chipnummer_avvises()
    {
        var h = await _fixture.OpprettHusstand("Chip A");
        await using var db = _fixture.LagContext(h);

        db.Dyr.Add(NyttDyr(h, "Luna", "578098100000001"));
        await db.SaveChangesAsync();

        db.Dyr.Add(NyttDyr(h, "Milo", "578098100000001"));

        var feil = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        var pg = Assert.IsType<PostgresException>(feil.InnerException);
        Assert.Equal("23505", pg.SqlState);
        Assert.Equal("ux_dyr_chip", pg.ConstraintName);
    }

    [Fact]
    public async Task To_dyr_uten_chipnummer_kan_lagres()
    {
        // Tom streng ma normaliseres til null for lagring. Er den "" i stedet,
        // kolliderer dyr nummer to - tom streng er en verdi, NULL deltar ikke
        // i unikhetssjekken. Se plan kapittel 5.3.
        var h = await _fixture.OpprettHusstand("Uten chip");
        await using var db = _fixture.LagContext(h);

        db.Dyr.Add(NyttDyr(h, "Luna"));
        db.Dyr.Add(NyttDyr(h, "Milo"));

        await db.SaveChangesAsync();

        Assert.Equal(2, await db.Dyr.CountAsync());
    }

    [Fact]
    public async Task Chipnummer_pa_deaktivert_dyr_blokkerer_fortsatt()
    {
        // Query-filteret skjuler deaktiverte dyr, men indeksen ser dem.
        var h = await _fixture.OpprettHusstand("Deaktivert chip");

        await using (var db = _fixture.LagContext(h))
        {
            var dyr = NyttDyr(h, "Luna", "578098100000002");
            dyr.Aktiv = false;
            db.Dyr.Add(dyr);
            await db.SaveChangesAsync();
        }

        await using var db2 = _fixture.LagContext(h);

        // Dyret er usynlig...
        Assert.Empty(await db2.Dyr.ToListAsync());

        // ...men chipnummeret er fortsatt opptatt.
        db2.Dyr.Add(NyttDyr(h, "Milo", "578098100000002"));

        var feil = await Assert.ThrowsAsync<DbUpdateException>(
            () => db2.SaveChangesAsync());

        Assert.Equal("23505", Assert.IsType<PostgresException>(feil.InnerException).SqlState);
    }

    [Fact]
    public async Task Deaktivert_dyr_lagres_faktisk_som_inaktivt()
    {
        // Kolonnen har DEFAULT TRUE. Skriver EF ikke ut aktiv = false
        // eksplisitt, slar databasens standardverdi inn og dyret forblir
        // aktivt - en deaktivering som stille ikke skjer.
        var h = await _fixture.OpprettHusstand("Sentinel");

        await using (var db = _fixture.LagContext(h))
        {
            var dyr = NyttDyr(h, "Luna");
            dyr.Aktiv = false;
            db.Dyr.Add(dyr);
            await db.SaveChangesAsync();
        }

        await using var db2 = _fixture.LagContext(h);

        var raden = await db2.Dyr
            .IgnoreQueryFilters()
            .SingleAsync(d => d.HusstandId == h);

        Assert.False(raden.Aktiv);
    }

    [Fact]
    public async Task Forplan_med_bade_prosent_og_gram_avvises()
    {
        var h = await _fixture.OpprettHusstand("Forplan verdi");
        await using var db = _fixture.LagContext(h);

        var dyr = NyttDyr(h, "Luna");
        db.Dyr.Add(dyr);
        await db.SaveChangesAsync();

        db.Forplan.Add(new Forplan
        {
            DyrId = dyr.Id,
            Metode = Formetode.Prosent,
            ProsentTidels = 50,
            GramPerDag = 400
        });

        var feil = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        var pg = Assert.IsType<PostgresException>(feil.InnerException);
        Assert.Equal("23514", pg.SqlState);
        Assert.Equal("ck_forplan_verdi", pg.ConstraintName);
    }

    [Fact]
    public async Task To_aktive_forplaner_pa_samme_dyr_avvises()
    {
        var h = await _fixture.OpprettHusstand("Forplan aktiv");
        await using var db = _fixture.LagContext(h);

        var dyr = NyttDyr(h, "Luna");
        db.Dyr.Add(dyr);
        await db.SaveChangesAsync();

        db.Forplan.Add(new Forplan
        {
            DyrId = dyr.Id,
            Metode = Formetode.Gram,
            GramPerDag = 400
        });
        await db.SaveChangesAsync();

        db.Forplan.Add(new Forplan
        {
            DyrId = dyr.Id,
            Metode = Formetode.Gram,
            GramPerDag = 500
        });

        var feil = await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync());

        var pg = Assert.IsType<PostgresException>(feil.InnerException);
        Assert.Equal("23505", pg.SqlState);
        Assert.Equal("ux_forplan_aktiv", pg.ConstraintName);
    }

    [Fact]
    public async Task Sletting_av_dyr_fjerner_vekt()
    {
        var h = await _fixture.OpprettHusstand("Kaskade");
        await using var db = _fixture.LagContext(h);

        var dyr = NyttDyr(h, "Luna");
        dyr.Vekter.Add(new Vekt { VektGram = 27400, Dato = new DateOnly(2026, 8, 1) });
        db.Dyr.Add(dyr);
        await db.SaveChangesAsync();

        Assert.Single(await db.Vekt.ToListAsync());

        db.Dyr.Remove(dyr);
        await db.SaveChangesAsync();

        Assert.Empty(await db.Vekt.IgnoreQueryFilters().Where(v => v.DyrId == dyr.Id).ToListAsync());
    }
}
