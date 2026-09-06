using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Den viktigste testen i prosjektet. Skrevet for forste funksjon, jf.
/// plan kapittel 17.3 og CLAUDE.md.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class IsolasjonsTester
{
    private readonly DatabaseFixture _fixture;

    public IsolasjonsTester(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Husstand_ser_ikke_annen_husstands_dyr()
    {
        var a = await _fixture.OpprettHusstand("Hjemme");
        var b = await _fixture.OpprettHusstand("Naboen");

        await using (var ctxA = _fixture.LagContext(a))
        {
            ctxA.Dyr.Add(new Dyr
            {
                HusstandId = a,
                Navn = "Luna",
                Art = Art.Hund,
                Kjonn = Kjonn.Tispe
            });
            await ctxA.SaveChangesAsync();
        }

        // Husstand B skal ikke se dyret.
        await using (var ctxB = _fixture.LagContext(b))
        {
            Assert.Empty(await ctxB.Dyr.ToListAsync());
            Assert.Null(await ctxB.Dyr.FirstOrDefaultAsync(d => d.Navn == "Luna"));
        }

        // ...men husstand A skal. Uten denne halvdelen ville et filter som
        // returnerer tomt for alle ha bestatt testen.
        await using var ctxAIgjen = _fixture.LagContext(a);
        var dyr = Assert.Single(await ctxAIgjen.Dyr.ToListAsync());
        Assert.Equal("Luna", dyr.Navn);
    }

    [Fact]
    public async Task Husstand_ser_ikke_annen_husstands_vekter()
    {
        // Vekt har ikke husstand_id selv - filteret gar via navigasjonen
        // v.Dyr.HusstandId. Den varianten ma testes for seg.
        var a = await _fixture.OpprettHusstand("Hjemme");
        var b = await _fixture.OpprettHusstand("Naboen");

        await using (var ctxA = _fixture.LagContext(a))
        {
            var dyr = new Dyr
            {
                HusstandId = a,
                Navn = "Milo",
                Art = Art.Katt,
                Kjonn = Kjonn.Hann
            };
            dyr.Vekter.Add(new Vekt
            {
                VektGram = 4200,
                Dato = new DateOnly(2026, 8, 1)
            });

            ctxA.Dyr.Add(dyr);
            await ctxA.SaveChangesAsync();
        }

        await using (var ctxB = _fixture.LagContext(b))
        {
            Assert.Empty(await ctxB.Vekt.ToListAsync());
        }

        await using var ctxAIgjen = _fixture.LagContext(a);
        Assert.Single(await ctxAIgjen.Vekt.ToListAsync());
    }

    [Fact]
    public async Task Husstand_ser_ikke_annen_husstands_forplantrinn()
    {
        // Torrfortabellen filtreres gjennom to ledd - t.Forplan.Dyr.HusstandId
        // - og den varianten ma testes for seg. Foringsmengden til et fremmed
        // dyr er ikke noe naboen skal kunne lese.
        var a = await _fixture.OpprettHusstand("Hjemme");
        var b = await _fixture.OpprettHusstand("Naboen");

        await using (var ctxA = _fixture.LagContext(a))
        {
            var dyr = new Dyr
            {
                HusstandId = a,
                Navn = "Luna",
                Art = Art.Hund,
                Kjonn = Kjonn.Tispe,
                Fodselsdato = new DateOnly(2026, 5, 1)
            };

            var plan = new Forplan
            {
                Metode = Formetode.Tabell,
                ProsentTidels = 50,
                VektdelAndelProsent = 70,
                AntallMaltider = 2
            };

            plan.Tabelltrinn.Add(new Forplantrinn { AlderMnd = 3, GramPerDag = 160 });
            plan.Tabelltrinn.Add(new Forplantrinn { AlderMnd = 4, GramPerDag = 180 });
            dyr.Forplaner.Add(plan);

            ctxA.Dyr.Add(dyr);
            await ctxA.SaveChangesAsync();
        }

        await using (var ctxB = _fixture.LagContext(b))
        {
            Assert.Empty(await ctxB.Forplantrinn.ToListAsync());
            Assert.Empty(await ctxB.Forplan.ToListAsync());
        }

        await using var ctxAIgjen = _fixture.LagContext(a);
        Assert.Equal(2, await ctxAIgjen.Forplantrinn.CountAsync());
    }

    [Fact]
    public async Task Uautentisert_kontekst_ser_ingenting()
    {
        // HusstandId 0 betyr "ikke satt". Fail closed - se ADR 0001.
        var a = await _fixture.OpprettHusstand("Hjemme");

        await using (var ctxA = _fixture.LagContext(a))
        {
            ctxA.Dyr.Add(new Dyr
            {
                HusstandId = a,
                Navn = "Nala",
                Art = Art.Hund,
                Kjonn = Kjonn.Tispe
            });
            await ctxA.SaveChangesAsync();
        }

        await using var utenHusstand = _fixture.LagContext(husstandId: 0);
        Assert.Empty(await utenHusstand.Dyr.ToListAsync());
    }
}
