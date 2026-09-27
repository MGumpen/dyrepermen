using System.Net;
using System.Text.RegularExpressions;
using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Services;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Dyrepermen.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Dyrets profilbilde og kjennetegn. Bildet er et dokument med kategori
/// Profilbilde, hoyst ett per dyr, lagret i databasen som vedleggene.
/// Se ADR 0018.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed partial class ProfilbildeTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public ProfilbildeTester(DatabaseFixture fixture) => _fixture = fixture;

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

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
    private static readonly byte[] Pdf = "%PDF-1.7\n"u8.ToArray();

    private static DokumentService Dokumenter(DyrepermenDbContext db, int husstand)
        => new(db, new Husstandskontekst { HusstandId = husstand },
            NullLogger<DokumentService>.Instance);

    private static DyrService Dyrtjeneste(DyrepermenDbContext db, int husstand)
        => new(db, new Husstandskontekst { HusstandId = husstand },
            NullLogger<DyrService>.Instance);

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

    // --- Tjenesten -------------------------------------------------------

    [Fact]
    public async Task Bildet_lagres_byttes_og_fjernes()
    {
        var h = await _fixture.OpprettHusstand("Profilbilde");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);
        var dokumenter = Dokumenter(db, h);
        var dyr = Dyrtjeneste(db, h);

        Assert.True((await dokumenter.LagreProfilbilde(
            dyrId, new NyttVedlegg("luna.jpg", Jpeg), default)).Ok);

        var forste = (await dyr.HentDetaljer(dyrId, default))!.ProfilbildeId;
        Assert.NotNull(forste);

        // Et nytt bilde bytter ut det gamle. Hoyst ett per dyr.
        Assert.True((await dokumenter.LagreProfilbilde(
            dyrId, new NyttVedlegg("luna2.png", Png), default)).Ok);

        var andre = (await dyr.HentDetaljer(dyrId, default))!.ProfilbildeId;
        Assert.NotEqual(forste, andre);
        Assert.Equal(1, await db.Dokument.CountAsync());
        Assert.Equal(1, await db.DokumentInnhold.CountAsync());
        Assert.Equal(Png, (await dokumenter.Hent(andre!.Value, default))!.Data);

        Assert.True(await dokumenter.FjernProfilbilde(dyrId, default));
        Assert.Null((await dyr.HentDetaljer(dyrId, default))!.ProfilbildeId);
        Assert.Equal(0, await db.DokumentInnhold.CountAsync());
    }

    [Fact]
    public async Task Pdf_kan_ikke_vaere_profilbilde()
    {
        var h = await _fixture.OpprettHusstand("Profil pdf");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        var resultat = await Dokumenter(db, h).LagreProfilbilde(
            dyrId, new NyttVedlegg("luna.pdf", Pdf), default);

        Assert.False(resultat.Ok);
        Assert.Contains("ikke et bilde", resultat.Feil);
        Assert.Equal(0, await db.Dokument.CountAsync());
    }

    /// <summary>
    /// Tjenesten bytter det gamle bildet ut, men indeksen er det som hindrer
    /// to profilbilder. Testen beviser at den finnes - uten den kunne to
    /// samtidige opplastinger gi to.
    /// </summary>
    [Fact]
    public async Task Databasen_tillater_ikke_to_profilbilder()
    {
        var h = await _fixture.OpprettHusstand("To profilbilder");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        for (var i = 0; i < 2; i++)
        {
            db.Dokument.Add(new Dokument
            {
                DyrId = dyrId,
                Originalnavn = $"luna{i}.jpg",
                Innholdstype = "image/jpeg",
                StorrelseByte = Jpeg.Length,
                Kategori = DokumentKategori.Profilbilde,
                OpplastetDato = new DateOnly(2026, 9, 1),
                Innhold = new DokumentInnhold { Data = Jpeg }
            });
        }

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Fullt_husstandstak_hindrer_ikke_a_bytte_bilde()
    {
        var h = await _fixture.OpprettHusstand("Bytt pa grensen");
        await using var db = _fixture.LagContext(h);
        var dyrId = await NyttDyr(db, h);

        // Et profilbilde som registrert tar hele plassen. Et nytt bilde av
        // samme storrelse skal likevel ga: det gamle forsvinner i samme slengen.
        db.Dokument.Add(new Dokument
        {
            DyrId = dyrId,
            Originalnavn = "stort.jpg",
            Innholdstype = "image/jpeg",
            StorrelseByte = (int)Vedleggsregler.MaksHusstandByte,
            Kategori = DokumentKategori.Profilbilde,
            OpplastetDato = new DateOnly(2026, 9, 1),
            Innhold = new DokumentInnhold { Data = Jpeg }
        });
        await db.SaveChangesAsync();

        Assert.True((await Dokumenter(db, h).LagreProfilbilde(
            dyrId, new NyttVedlegg("nytt.jpg", Jpeg), default)).Ok);
    }

    [Fact]
    public async Task En_annen_husstand_kan_verken_sette_eller_fjerne_bildet()
    {
        var a = await _fixture.OpprettHusstand("Profil A");
        var b = await _fixture.OpprettHusstand("Profil B");

        int dyrId;
        await using (var eier = _fixture.LagContext(a))
        {
            dyrId = await NyttDyr(eier, a);
            await Dokumenter(eier, a).LagreProfilbilde(
                dyrId, new NyttVedlegg("luna.jpg", Jpeg), default);
        }

        await using (var fremmed = _fixture.LagContext(b))
        {
            Assert.True((await Dokumenter(fremmed, b).LagreProfilbilde(
                dyrId, new NyttVedlegg("annen.jpg", Png), default)).FinnesIkke);
            Assert.False(await Dokumenter(fremmed, b).FjernProfilbilde(dyrId, default));
        }

        await using var igjen = _fixture.LagContext(a);
        Assert.Equal("luna.jpg", await igjen.Dokument.Select(d => d.Originalnavn).SingleAsync());
    }

    // --- Over HTTP -------------------------------------------------------

    [Fact]
    public async Task Bildet_vises_pa_oversikten_og_pa_dyrets_side()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        // Uten bilde: forbokstaven, ingen lenke til et dokument.
        var for_ = await (await klient.Hent("/")).Content.ReadAsStringAsync();
        Assert.Contains("tomt-bilde", for_);
        Assert.DoesNotMatch(Bildelenke(), for_);

        var svar = await klient.PostMedFiler($"/dyr/{dyrId}/profilbilde", [],
            [("bilde", "luna.jpg", Jpeg)], tokenFra: $"/dyr/{dyrId}/rediger");
        Assert.True(Skjemaklient.GikkGjennom(svar));
        Assert.EndsWith("#profilbilde", svar.Headers.Location?.ToString());

        var oversikt = await (await klient.Hent("/")).Content.ReadAsStringAsync();
        var lenke = Bildelenke().Match(oversikt);
        Assert.True(lenke.Success, "Fant ikke profilbildet pa oversikten.");

        var side = await (await klient.Hent($"/dyr/{dyrId}")).Content.ReadAsStringAsync();
        Assert.Contains(lenke.Value, side);

        var fil = await klient.Hent(lenke.Groups[1].Value);
        Assert.Equal("image/jpeg", fil.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// Selve beskjaeringen skjer i nettleseren, og HTTP-testene kjorer ikke
    /// skript. Det testen kan fange, er at feltet, flaten og skriptet henger
    /// sammen: uten data-beskjaer, eller med feil id pa redigereren, faller
    /// siden stille tilbake til a laste opp originalen.
    /// </summary>
    [Fact]
    public async Task Redigeringssiden_har_beskjaering_av_bildet()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var side = await (await klient.Hent($"/dyr/{dyrId}/rediger")).Content.ReadAsStringAsync();

        Assert.Contains("data-beskjaer=\"800\"", side);
        Assert.Contains("data-beskjaer-redigerer=\"beskjaer\"", side);
        Assert.Contains("id=\"beskjaer\"", side);
        Assert.Contains("/js/beskjaering.js", side);
    }

    [Fact]
    public async Task Farge_og_kjennetegn_lagres_og_vises()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var svar = await klient.Post($"/dyr/{dyrId}/rediger", new Dictionary<string, string>
        {
            ["Navn"] = "Luna",
            ["Art"] = ((int)Art.Hund).ToString(),
            ["Kjonn"] = ((int)Kjonn.Tispe).ToString(),
            ["Farge"] = "Gul, korthåret",
            ["Kjennetegn"] = "Hvit flekk på brystet",
            ["ForplanAktiv"] = "true",
            ["ForingsloggAktiv"] = "false"
        });
        Assert.True(Skjemaklient.GikkGjennom(svar),
            $"Endringen ble ikke lagret: {await Skjemaklient.Feilmeldinger(svar)}");

        var side = WebUtility.HtmlDecode(
            await (await klient.Hent($"/dyr/{dyrId}")).Content.ReadAsStringAsync());
        Assert.Contains("Gul, korthåret", side);
        Assert.Contains("Hvit flekk på brystet", side);
    }

    [GeneratedRegex(""""src="(/dokument/\d+)"""")]
    private static partial Regex Bildelenke();
}
