using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Dyrepermen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Husstandsmedlemskap, roller og invitasjoner. Se ADR 0009.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed class HusstandTester
{
    private readonly DatabaseFixture _fixture;

    public HusstandTester(DatabaseFixture fixture) => _fixture = fixture;

    private static HusstandService Tjeneste(DyrepermenDbContext db, int husstandId)
        => new(db,
            new Dyrepermen.Application.Services.Husstandskontekst { HusstandId = husstandId },
            NullLogger<HusstandService>.Instance);

    private static async Task<int> NyBruker(DyrepermenDbContext db, string epost)
    {
        var bruker = new Bruker
        {
            UserName = epost,
            NormalizedUserName = epost.ToUpperInvariant(),
            Email = epost,
            NormalizedEmail = epost.ToUpperInvariant(),
            Visningsnavn = epost.Split('@')[0],
            SecurityStamp = Guid.NewGuid().ToString()
        };
        db.Users.Add(bruker);
        await db.SaveChangesAsync();
        return bruker.Id;
    }

    private static async Task Meld(
        DyrepermenDbContext db, int husstandId, int brukerId, Husstandsrolle rolle)
    {
        db.Husstandsmedlemskap.Add(new Husstandsmedlemskap
        {
            HusstandId = husstandId,
            BrukerId = brukerId,
            Rolle = rolle
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Bruker_kan_vaere_med_i_flere_husstander_med_ulik_rolle()
    {
        // Scenarioet: du eier din egen husstand, og er gjest i din fars der
        // du passer hunden hans.
        var min = await _fixture.OpprettHusstand("Hjemme");
        var pappa = await _fixture.OpprettHusstand("Hos pappa");

        await using var db = _fixture.LagContext(min);
        var meg = await NyBruker(db, "meg@eksempel.no");

        await Meld(db, min, meg, Husstandsrolle.Eier);
        await Meld(db, pappa, meg, Husstandsrolle.Gjest);

        var medlemskap = await db.Husstandsmedlemskap
            .Where(m => m.BrukerId == meg)
            .OrderBy(m => m.HusstandId)
            .ToListAsync();

        Assert.Equal(2, medlemskap.Count);
        Assert.Equal(Husstandsrolle.Eier, medlemskap.Single(m => m.HusstandId == min).Rolle);
        Assert.Equal(Husstandsrolle.Gjest, medlemskap.Single(m => m.HusstandId == pappa).Rolle);
    }

    [Fact]
    public async Task Aa_legge_noen_til_tar_ikke_medlemskapet_deres_andre_steder()
    {
        // Dette erstatter den gamle testen "e-post som tilhorer annen
        // husstand avvises". Sjekken var nodvendig da husstand_id var EN
        // kolonne: a legge noen til flyttet dem ut av sin egen husstand.
        //
        // Med medlemskap i egen tabell finnes ikke den faren. Testen
        // verifiserer at det faktisk stemmer - at fjerningen av sjekken var
        // trygg, ikke bare praktisk. Se ADR 0009.
        var min = await _fixture.OpprettHusstand("Min husstand");
        var deres = await _fixture.OpprettHusstand("Deres husstand");

        await using var db = _fixture.LagContext(min);
        var jeg = await NyBruker(db, "jeg@eksempel.no");
        var annen = await NyBruker(db, "annen@eksempel.no");

        await Meld(db, min, jeg, Husstandsrolle.Eier);
        await Meld(db, deres, annen, Husstandsrolle.Eier);

        var resultat = await Tjeneste(db, min)
            .LeggTilMedlem("ANNEN@eksempel.no", Husstandsrolle.Gjest, jeg, default);

        Assert.Equal(LeggTilResultat.LagtTil, resultat);

        var deres_etterpa = await db.Husstandsmedlemskap
            .SingleAsync(m => m.BrukerId == annen && m.HusstandId == deres);

        // Uroert - og fortsatt eier der.
        Assert.Equal(Husstandsrolle.Eier, deres_etterpa.Rolle);

        // Og na ogsa gjest hos meg.
        var hos_meg = await db.Husstandsmedlemskap
            .SingleAsync(m => m.BrukerId == annen && m.HusstandId == min);
        Assert.Equal(Husstandsrolle.Gjest, hos_meg.Rolle);
    }

    [Fact]
    public async Task Samme_person_kan_ikke_legges_til_to_ganger()
    {
        var h = await _fixture.OpprettHusstand("Dublett");

        await using var db = _fixture.LagContext(h);
        var eier = await NyBruker(db, "eier@eksempel.no");
        var gjest = await NyBruker(db, "gjest@eksempel.no");
        await Meld(db, h, eier, Husstandsrolle.Eier);

        var tjeneste = Tjeneste(db, h);
        Assert.Equal(LeggTilResultat.LagtTil,
            await tjeneste.LeggTilMedlem("gjest@eksempel.no", Husstandsrolle.Gjest, eier, default));
        Assert.Equal(LeggTilResultat.AlleredeMedlem,
            await tjeneste.LeggTilMedlem("gjest@eksempel.no", Husstandsrolle.Gjest, eier, default));

        Assert.Equal(1, await db.Husstandsmedlemskap
            .CountAsync(m => m.BrukerId == gjest && m.HusstandId == h));
    }

    [Fact]
    public async Task Invitasjon_baerer_rollen_og_loses_inn_ved_registrering()
    {
        var h = await _fixture.OpprettHusstand("Inviterer");

        await using var db = _fixture.LagContext(h);
        var eier = await NyBruker(db, "eier2@eksempel.no");
        await Meld(db, h, eier, Husstandsrolle.Eier);

        var tjeneste = Tjeneste(db, h);
        Assert.Equal(LeggTilResultat.VenterPaRegistrering,
            await tjeneste.LeggTilMedlem("Ny@Eksempel.no", Husstandsrolle.Gjest, eier, default));

        // Adressen normaliseres til sma bokstaver ved lagring.
        var invitasjon = await db.HusstandInvitasjon.SingleAsync();
        Assert.Equal("ny@eksempel.no", invitasjon.Epost);
        Assert.Equal(Husstandsrolle.Gjest, invitasjon.Rolle);

        var nyId = await NyBruker(db, "ny@eksempel.no");
        Assert.True(await tjeneste.LosInnInvitasjon(nyId, "Ny@Eksempel.no", default));

        var medlemskap = await db.Husstandsmedlemskap
            .SingleAsync(m => m.BrukerId == nyId && m.HusstandId == h);

        // Rollen fra invitasjonen folger med.
        Assert.Equal(Husstandsrolle.Gjest, medlemskap.Rolle);
    }

    [Fact]
    public async Task Siste_eier_kan_ikke_fjernes_eller_degraderes()
    {
        // En husstand uten eier ville hatt last innstillingsside for alle -
        // gjester kan ikke endre den.
        var h = await _fixture.OpprettHusstand("Siste eier");

        await using var db = _fixture.LagContext(h);
        var eier = await NyBruker(db, "enesteeier@eksempel.no");
        var gjest = await NyBruker(db, "engjest@eksempel.no");
        await Meld(db, h, eier, Husstandsrolle.Eier);
        await Meld(db, h, gjest, Husstandsrolle.Gjest);

        var tjeneste = Tjeneste(db, h);

        Assert.False(await tjeneste.FjernMedlem(eier, default));
        Assert.False(await tjeneste.EndreRolle(eier, Husstandsrolle.Gjest, default));

        // Gjesten kan derimot fjernes.
        Assert.True(await tjeneste.FjernMedlem(gjest, default));
    }

    [Fact]
    public async Task Gjest_kan_forfremmes_til_eier_og_da_kan_den_forste_gaa()
    {
        var h = await _fixture.OpprettHusstand("Overdragelse");

        await using var db = _fixture.LagContext(h);
        var forste = await NyBruker(db, "forste@eksempel.no");
        var andre = await NyBruker(db, "andre@eksempel.no");
        await Meld(db, h, forste, Husstandsrolle.Eier);
        await Meld(db, h, andre, Husstandsrolle.Gjest);

        var tjeneste = Tjeneste(db, h);

        Assert.True(await tjeneste.EndreRolle(andre, Husstandsrolle.Eier, default));
        Assert.True(await tjeneste.FjernMedlem(forste, default));

        var igjen = await db.Husstandsmedlemskap.SingleAsync(m => m.HusstandId == h);
        Assert.Equal(andre, igjen.BrukerId);
        Assert.Equal(Husstandsrolle.Eier, igjen.Rolle);
    }

    [Fact]
    public async Task Sletting_av_bruker_beholder_vektradene_med_null_i_registrert_av()
    {
        // Personopplysninger slettes, husstandens data avidentifiseres. En
        // kaskadesletting ville tatt med seg hele vekthistorikken til hunden.
        // Se plan kapittel 12.5.
        var a = await _fixture.OpprettHusstand("Avidentifisering");

        await using var db = _fixture.LagContext(a);

        var bruker = await NyBruker(db, "forsvinner@eksempel.no");
        await Meld(db, a, bruker, Husstandsrolle.Eier);

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

        await db.Users.Where(u => u.Id == bruker).ExecuteDeleteAsync();
        db.ChangeTracker.Clear();

        var vekt = await db.Vekt.SingleAsync(v => v.DyrId == dyr.Id);

        Assert.Equal(27400, vekt.VektGram);
        Assert.Null(vekt.RegistrertAvBrukerId);

        // Medlemskapet forsvinner derimot med brukeren.
        Assert.Equal(0, await db.Husstandsmedlemskap.CountAsync(m => m.BrukerId == bruker));
    }
}
