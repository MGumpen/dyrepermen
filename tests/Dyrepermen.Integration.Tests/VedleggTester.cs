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
/// Kvitteringer og andre filer lagt ved et veterinaerbesok, lagret i
/// databasen. Se ADR 0018 og plan kapittel 15: kun jpg, png og pdf, filer
/// serveres gjennom en controller som verifiserer husstand, og en fremmed id
/// gir 404.
/// </summary>
[Collection(Databasesamling.Navn)]
public sealed partial class VedleggTester : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Appfabrikk _app = null!;

    public VedleggTester(DatabaseFixture fixture) => _fixture = fixture;

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
    private static readonly byte[] Pdf = "%PDF-1.7\n%kvittering\n"u8.ToArray();
    private static readonly byte[] Program = [0x4D, 0x5A, 0x90, 0x00, 0x03];

    private static DateOnly Idag => Tidssone.Idag(DateTimeOffset.UtcNow);

    private static DokumentService Tjeneste(
        DyrepermenDbContext db, int husstand, bool demo = false)
        => new(db,
            new Husstandskontekst { HusstandId = husstand, ErDemo = demo },
            NullLogger<DokumentService>.Instance);

    private static VeterinarService Veterinar(DyrepermenDbContext db, int husstand)
        => new(db, new Husstandskontekst { HusstandId = husstand });

    /// <summary>Et dyr med ett besok. Returnerer besokets id.</summary>
    private static async Task<int> NyttBesok(DyrepermenDbContext db, int husstand)
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

        return (await Veterinar(db, husstand).OpprettBesok(new NyttVetbesok(
            dyr.Id, null, "Dyreklinikken", Idag, null, "Vaksine",
            null, 850, false, null, null, null), default))!.Value;
    }

    // --- Tjenesten -------------------------------------------------------

    [Fact]
    public async Task Kvitteringen_lagres_og_kan_hentes_igjen()
    {
        var h = await _fixture.OpprettHusstand("Kvittering");
        await using var db = _fixture.LagContext(h);
        var besokId = await NyttBesok(db, h);
        var tjeneste = Tjeneste(db, h);

        var resultat = await tjeneste.LeggVedBesok(besokId,
            [new NyttVedlegg("kvittering.jpg", Jpeg), new NyttVedlegg("side2.pdf", Pdf)],
            default);

        Assert.True(resultat.Ok);

        var besok = await Veterinar(db, h).HentEttBesok(besokId, default);
        Assert.Equal(["kvittering.jpg", "side2.pdf"], besok!.Vedlegg.Select(v => v.Navn));
        Assert.True(besok.Vedlegg[0].ErBilde);
        Assert.False(besok.Vedlegg[1].ErBilde);

        var fil = await tjeneste.Hent(besok.Vedlegg[0].Id, default);
        Assert.Equal("image/jpeg", fil!.Innholdstype);
        Assert.Equal(Jpeg, fil.Data);

        var dokument = await db.Dokument.SingleAsync(d => d.Id == besok.Vedlegg[0].Id);
        Assert.Equal(DokumentKategori.Kvittering, dokument.Kategori);
        Assert.Equal(Jpeg.Length, dokument.StorrelseByte);
    }

    [Fact]
    public async Task Feil_filtype_avvises_og_ingenting_lagres()
    {
        var h = await _fixture.OpprettHusstand("Feil type");
        await using var db = _fixture.LagContext(h);
        var besokId = await NyttBesok(db, h);

        // Ett gyldig vedlegg og ett program forkledd som bilde: ingen av dem
        // skal lagres. Enten kommer hele kvitteringen med, eller ingenting.
        var resultat = await Tjeneste(db, h).LeggVedBesok(besokId,
            [new NyttVedlegg("side1.jpg", Jpeg), new NyttVedlegg("side2.jpg", Program)],
            default);

        Assert.False(resultat.Ok);
        Assert.Contains("side2.jpg", resultat.Feil);
        Assert.Equal(0, await db.Dokument.CountAsync());
    }

    [Fact]
    public async Task Besok_i_en_annen_husstand_kan_ikke_fa_vedlegg()
    {
        var a = await _fixture.OpprettHusstand("Vedlegg A");
        var b = await _fixture.OpprettHusstand("Vedlegg B");

        int besokId;
        await using (var eier = _fixture.LagContext(a))
        {
            besokId = await NyttBesok(eier, a);
        }

        await using (var fremmed = _fixture.LagContext(b))
        {
            var resultat = await Tjeneste(fremmed, b).LeggVedBesok(
                besokId, [new NyttVedlegg("kvittering.jpg", Jpeg)], default);

            Assert.True(resultat.FinnesIkke);
        }

        await using var igjen = _fixture.LagContext(a);
        Assert.Equal(0, await igjen.Dokument.CountAsync());
    }

    [Fact]
    public async Task En_annen_husstand_kan_verken_hente_eller_slette_vedlegget()
    {
        var a = await _fixture.OpprettHusstand("Hent A");
        var b = await _fixture.OpprettHusstand("Hent B");

        int dokumentId;
        await using (var eier = _fixture.LagContext(a))
        {
            var besokId = await NyttBesok(eier, a);
            await Tjeneste(eier, a).LeggVedBesok(
                besokId, [new NyttVedlegg("kvittering.jpg", Jpeg)], default);
            dokumentId = await eier.Dokument.Select(d => d.Id).SingleAsync();
        }

        await using (var fremmed = _fixture.LagContext(b))
        {
            Assert.Null(await Tjeneste(fremmed, b).Hent(dokumentId, default));
            Assert.False(await Tjeneste(fremmed, b).Slett(dokumentId, default));
        }

        await using var igjen = _fixture.LagContext(a);
        Assert.NotNull(await Tjeneste(igjen, a).Hent(dokumentId, default));
    }

    [Fact]
    public async Task Slettet_time_tar_med_seg_vedleggene()
    {
        var h = await _fixture.OpprettHusstand("Slett time");
        await using var db = _fixture.LagContext(h);
        var besokId = await NyttBesok(db, h);

        await Tjeneste(db, h).LeggVedBesok(
            besokId, [new NyttVedlegg("kvittering.jpg", Jpeg)], default);

        Assert.True(await Veterinar(db, h).SlettBesok(besokId, default));

        Assert.Equal(0, await db.Dokument.CountAsync());
        Assert.Equal(0, await db.DokumentInnhold.CountAsync());
    }

    [Fact]
    public async Task Slettet_vedlegg_tar_med_seg_filen()
    {
        var h = await _fixture.OpprettHusstand("Slett vedlegg");
        await using var db = _fixture.LagContext(h);
        var besokId = await NyttBesok(db, h);
        var tjeneste = Tjeneste(db, h);

        await tjeneste.LeggVedBesok(besokId, [new NyttVedlegg("kvittering.jpg", Jpeg)], default);
        var id = await db.Dokument.Select(d => d.Id).SingleAsync();

        Assert.True(await tjeneste.Slett(id, default));
        Assert.Equal(0, await db.DokumentInnhold.CountAsync());
    }

    [Fact]
    public async Task Husstandens_tak_stopper_nye_vedlegg()
    {
        var h = await _fixture.OpprettHusstand("Fullt");
        await using var db = _fixture.LagContext(h);
        var besokId = await NyttBesok(db, h);
        var dyrId = await db.Vetbesok.Select(v => v.DyrId).SingleAsync();

        // Et vedlegg som tar nesten hele plassen. Storrelsen er det som
        // teller - innholdet trenger ikke vaere stort for testen.
        db.Dokument.Add(new Dokument
        {
            DyrId = dyrId,
            VetbesokId = besokId,
            Originalnavn = "stor.pdf",
            Innholdstype = "application/pdf",
            StorrelseByte = (int)Vedleggsregler.MaksHusstandByte - 2,
            Kategori = DokumentKategori.Kvittering,
            OpplastetDato = Idag,
            Innhold = new DokumentInnhold { Data = Pdf }
        });
        await db.SaveChangesAsync();

        var feil = await Tjeneste(db, h).Kontroller(
            [new NyttVedlegg("kvittering.jpg", Jpeg)], default);

        Assert.Equal(
            "Det er ikke plass til flere vedlegg. Samlet kan vedleggene i en husstand være høyst 50 MB.",
            feil);
    }

    [Fact]
    public async Task Demoen_kan_ikke_laste_opp()
    {
        var h = await _fixture.OpprettHusstand("Demo vedlegg");
        await using var db = _fixture.LagContext(h);
        var besokId = await NyttBesok(db, h);

        var resultat = await Tjeneste(db, h, demo: true).LeggVedBesok(
            besokId, [new NyttVedlegg("kvittering.jpg", Jpeg)], default);

        Assert.False(resultat.Ok);
        Assert.Equal("Filer kan ikke lastes opp i demoen.", resultat.Feil);
        Assert.Equal(0, await db.Dokument.CountAsync());
    }

    // --- Over HTTP -------------------------------------------------------

    private static Dictionary<string, string> Time(int dyrId) => new()
    {
        ["Ny.DyrId"] = dyrId.ToString(),
        ["Ny.Dato"] = Idag.ToString("yyyy-MM-dd"),
        ["Ny.Arsak"] = "Vaksine",
        ["Ny.KostnadKr"] = "850"
    };

    [Fact]
    public async Task Kvitteringen_lastes_opp_med_timen_og_vises_i_nettleseren()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var skjema = await (await klient.Hent("/veterinar/time/ny")).Content.ReadAsStringAsync();
        Assert.Contains("enctype=\"multipart/form-data\"", skjema);
        Assert.Contains("name=\"vedlegg\"", skjema);

        var svar = await klient.PostMedFiler("/veterinar/time", Time(dyrId),
            [("vedlegg", "kvittering.jpg", Jpeg)], tokenFra: "/veterinar/time/ny");

        Assert.True(Skjemaklient.GikkGjennom(svar),
            $"Timen ble ikke lagret: {await Skjemaklient.Feilmeldinger(svar)}");

        var liste = await (await klient.Hent("/veterinar")).Content.ReadAsStringAsync();
        var lenke = Dokumentlenke().Match(liste);
        Assert.True(lenke.Success, "Fant ikke lenken til kvitteringen pa veterinaersiden.");

        var fil = await klient.Hent(lenke.Value);
        Assert.Equal(HttpStatusCode.OK, fil.StatusCode);
        Assert.Equal("image/jpeg", fil.Content.Headers.ContentType?.MediaType);
        Assert.Equal("inline", fil.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal(Jpeg, await fil.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Ugyldig_fil_gir_feilmelding_og_ingen_time()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);

        var svar = await klient.PostMedFiler("/veterinar/time", Time(dyrId),
            [("vedlegg", "kvittering.jpg", Program)], tokenFra: "/veterinar/time/ny");

        Assert.Equal(HttpStatusCode.OK, svar.StatusCode);
        Assert.Contains("er ikke et bilde eller en PDF",
            await Skjemaklient.Feilmeldinger(svar));

        // Timen ble ikke lagret heller - brukeren skal ikke sitte igjen med
        // en time uten den kvitteringen hun trodde var med.
        var liste = WebUtility.HtmlDecode(
            await (await klient.Hent("/veterinar")).Content.ReadAsStringAsync());
        Assert.Contains("Ingen besøk registrert.", liste);
    }

    [Fact]
    public async Task Fremmed_husstands_fil_gir_404()
    {
        var eier = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(eier);
        await eier.PostMedFiler("/veterinar/time", Time(dyrId),
            [("vedlegg", "kvittering.jpg", Jpeg)], tokenFra: "/veterinar/time/ny");

        var liste = await (await eier.Hent("/veterinar")).Content.ReadAsStringAsync();
        var lenke = Dokumentlenke().Match(liste).Value;

        var fremmed = await Testoppsett.InnloggetKlient(_app);
        Assert.Equal(HttpStatusCode.NotFound, (await fremmed.Hent(lenke)).StatusCode);
    }

    [Fact]
    public async Task Vedlegget_kan_slettes_fra_timen()
    {
        var klient = await Testoppsett.InnloggetKlient(_app);
        var dyrId = await Testoppsett.NyttDyr(klient);
        await klient.PostMedFiler("/veterinar/time", Time(dyrId),
            [("vedlegg", "kvittering.jpg", Jpeg)], tokenFra: "/veterinar/time/ny");

        var liste = await (await klient.Hent("/veterinar")).Content.ReadAsStringAsync();
        var rediger = Regex.Match(liste, """href="(/veterinar/time/\d+/rediger)""").Groups[1].Value;

        var side = await (await klient.Hent(rediger)).Content.ReadAsStringAsync();
        var slett = Regex.Match(side, """action="(/veterinar/time/\d+/vedlegg/\d+/slett)""");
        Assert.True(slett.Success, "Fant ikke sletteknappen for vedlegget.");

        var svar = await klient.Post(slett.Groups[1].Value, [], tokenFra: rediger);
        Assert.True(Skjemaklient.GikkGjennom(svar));

        var etter = await (await klient.Hent("/veterinar")).Content.ReadAsStringAsync();
        Assert.DoesNotMatch(Dokumentlenke(), etter);
    }

    [GeneratedRegex("""/dokument/\d+""")]
    private static partial Regex Dokumentlenke();
}
