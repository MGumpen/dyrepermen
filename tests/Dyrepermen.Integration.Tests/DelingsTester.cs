using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Akseptansekriteriet «to brukere i samme husstand ser de samme dyrene»
/// fra plan kapittel 16, fase 1.
///
/// Poenget er hele produktet: to voksne skal dele alt om alle dyrene.
/// IsolasjonsTester viser at husstander ikke ser hverandre - denne viser at
/// medlemmer i samme husstand faktisk deler. Uten begge er halve garantien
/// udekket.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class DelingsTester
{
    private readonly DatabaseFixture _fixture;

    public DelingsTester(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task To_brukere_i_samme_husstand_ser_samme_dyr()
    {
        var husstand = await _fixture.OpprettHusstand("Hjemme");

        // Bruker A registrerer dyret.
        await using (var a = _fixture.LagContext(husstand))
        {
            a.Dyr.Add(new Dyr
            {
                HusstandId = husstand,
                Navn = "Luna",
                Art = Art.Hund,
                Kjonn = Kjonn.Tispe
            });
            await a.SaveChangesAsync();
        }

        // Bruker B har egen forespørsel og egen kontekst, men samme husstand.
        await using var b = _fixture.LagContext(husstand);

        var dyr = Assert.Single(await b.Dyr.ToListAsync());
        Assert.Equal("Luna", dyr.Navn);
    }

    [Fact]
    public async Task Deaktivert_dyr_forsvinner_fra_listen_men_ikke_fra_databasen()
    {
        var husstand = await _fixture.OpprettHusstand("Deaktivering");

        int dyrId;
        await using (var ctx = _fixture.LagContext(husstand))
        {
            var dyr = new Dyr
            {
                HusstandId = husstand,
                Navn = "Milo",
                Art = Art.Katt,
                Kjonn = Kjonn.Hann
            };
            ctx.Dyr.Add(dyr);
            await ctx.SaveChangesAsync();
            dyrId = dyr.Id;

            // Slik DyrService.Deaktiver gjor det.
            dyr.Aktiv = false;
            await ctx.SaveChangesAsync();
        }

        await using var etterpa = _fixture.LagContext(husstand);

        // Borte fra oversikten...
        Assert.Empty(await etterpa.Dyr.ToListAsync());

        // ...men raden lever, og historikken med den.
        var bevart = await etterpa.Dyr
            .IgnoreQueryFilters()
            .SingleAsync(d => d.Id == dyrId);

        Assert.False(bevart.Aktiv);
        Assert.Equal("Milo", bevart.Navn);
    }
}
