using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Dyrepermen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dyrepermen.Integration.Tests;

/// <summary>Akseptansekriteriene for fase 6c, plan kapittel 16.</summary>
[Collection(Databasesamling.Navn)]
public sealed class HusstandTester
{
    private readonly DatabaseFixture _fixture;

    public HusstandTester(DatabaseFixture fixture) => _fixture = fixture;

    private static HusstandService Tjeneste(DyrepermenDbContext db, int husstandId)
        => new(db,
            new Dyrepermen.Application.Services.Husstandskontekst { HusstandId = husstandId },
            NullLogger<HusstandService>.Instance);

    private static async Task<int> NyBruker(
        DyrepermenDbContext db, string epost, int? husstandId)
    {
        var bruker = new Bruker
        {
            UserName = epost,
            NormalizedUserName = epost.ToUpperInvariant(),
            Email = epost,
            NormalizedEmail = epost.ToUpperInvariant(),
            Visningsnavn = epost.Split('@')[0],
            HusstandId = husstandId,
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(bruker);
        await db.SaveChangesAsync();
        return bruker.Id;
    }

    [Fact]
    public async Task Epost_som_tilhorer_annen_husstand_avvises()
    {
        // ---------------------------------------------------------------
        // Den viktigste testen i fase 6c.
        //
        // Uten sjekken i LeggTilMedlem kan hvem som helst taste inn
        // e-postadressen til en fremmed og flytte dem ut av deres egen
        // husstand. Er offeret siste medlem, blir alle dataene deres
        // utilgjengelige. Se plan kapittel 12.3.
        // ---------------------------------------------------------------
        var a = await _fixture.OpprettHusstand("Angriper");
        var b = await _fixture.OpprettHusstand("Offer");

        await using var db = _fixture.LagContext(a);
        var offer = await NyBruker(db, "offer@eksempel.no", b);
        var angriper = await NyBruker(db, "angriper@eksempel.no", a);

        var resultat = await Tjeneste(db, a)
            .LeggTilMedlem("offer@eksempel.no", angriper, default);

        Assert.Equal(LeggTilResultat.TilhorerAnnenHusstand, resultat);

        // Offeret skal fortsatt tilhore sin egen husstand.
        var etterpa = await db.Users.SingleAsync(u => u.Id == offer);
        Assert.Equal(b, etterpa.HusstandId);
    }

    [Fact]
    public async Task Bruker_uten_husstand_legges_til_direkte()
    {
        var a = await _fixture.OpprettHusstand("Mottaker");

        await using var db = _fixture.LagContext(a);
        var eier = await NyBruker(db, "eier1@eksempel.no", a);
        var ny = await NyBruker(db, "hjemlos@eksempel.no", null);

        var resultat = await Tjeneste(db, a)
            .LeggTilMedlem("HJEMLOS@eksempel.no", eier, default);

        Assert.Equal(LeggTilResultat.LagtTil, resultat);
        Assert.Equal(a, (await db.Users.SingleAsync(u => u.Id == ny)).HusstandId);
    }

    [Fact]
    public async Task Ukjent_adresse_gir_invitasjon_som_loses_inn_ved_registrering()
    {
        var a = await _fixture.OpprettHusstand("Inviterer");

        await using var db = _fixture.LagContext(a);
        var eier = await NyBruker(db, "eier2@eksempel.no", a);
        var tjeneste = Tjeneste(db, a);

        var resultat = await tjeneste
            .LeggTilMedlem("Fremtidig@Eksempel.no", eier, default);

        Assert.Equal(LeggTilResultat.VenterPaRegistrering, resultat);

        // Adressen normaliseres til sma bokstaver ved lagring.
        var invitasjon = await db.HusstandInvitasjon.SingleAsync();
        Assert.Equal("fremtidig@eksempel.no", invitasjon.Epost);

        // Personen registrerer seg senere - og lander rett i husstanden.
        var nyBrukerId = await NyBruker(db, "fremtidig@eksempel.no", null);
        Assert.True(await tjeneste.LosInnInvitasjon(
            nyBrukerId, "Fremtidig@Eksempel.no", default));

        Assert.Equal(a, (await db.Users.SingleAsync(u => u.Id == nyBrukerId)).HusstandId);

        var innlost = await db.HusstandInvitasjon.SingleAsync();
        Assert.Equal(nyBrukerId, innlost.InnlostAvBrukerId);
        Assert.NotNull(innlost.InnlostTid);
    }

    [Fact]
    public async Task Siste_medlem_kan_ikke_fjernes()
    {
        // En husstand uten medlemmer gjor dataene utilgjengelige for alltid
        // uten a bli slettet.
        var a = await _fixture.OpprettHusstand("Alene");

        await using var db = _fixture.LagContext(a);
        var alene = await NyBruker(db, "alene@eksempel.no", a);

        Assert.False(await Tjeneste(db, a).FjernMedlem(alene, default));
        Assert.Equal(a, (await db.Users.SingleAsync(u => u.Id == alene)).HusstandId);
    }

    [Fact]
    public async Task Sletting_av_bruker_beholder_vektradene_med_null_i_registrert_av()
    {
        // Akseptansekriteriet: personopplysninger slettes, husstandens data
        // avidentifiseres. En kaskadesletting ville tatt med seg hele
        // vekthistorikken til hunden. Se plan kapittel 12.5.
        var a = await _fixture.OpprettHusstand("Avidentifisering");

        await using var db = _fixture.LagContext(a);

        var bruker = await NyBruker(db, "forsvinner@eksempel.no", a);
        await NyBruker(db, "blir@eksempel.no", a);

        var dyr = new Dyr
        {
            HusstandId = a,
            Navn = "Luna",
            Art = Art.Hund,
            Kjonn = Kjonn.Tispe
        };
        dyr.Vekter.Add(new Vekt
        {
            VektGram = 27400,
            Dato = new DateOnly(2026, 8, 1),
            RegistrertAvBrukerId = bruker
        });
        db.Dyr.Add(dyr);
        await db.SaveChangesAsync();

        // Sletter brukerraden direkte - ON DELETE SET NULL i skjemaet er det
        // som skal handtere avidentifiseringen.
        await db.Users.Where(u => u.Id == bruker).ExecuteDeleteAsync();

        db.ChangeTracker.Clear();

        var vekt = await db.Vekt.SingleAsync(v => v.DyrId == dyr.Id);

        Assert.Equal(27400, vekt.VektGram);
        Assert.Null(vekt.RegistrertAvBrukerId);
    }
}
