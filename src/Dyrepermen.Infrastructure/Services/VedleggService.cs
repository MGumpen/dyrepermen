using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dyrepermen.Infrastructure.Services;

/// <summary>
/// Vedlegg til veterinaerbesok, lagret i databasen. Se ADR 0018.
///
/// Query-filtrene gjor husstandsavgrensningen, som ellers i appen - med ett
/// bevisst unntak: taket for hele databasen summeres pa tvers av
/// husstandene, se <see cref="Plassfeil"/>.
/// </summary>
public sealed class VedleggService : IVedleggService
{
    private readonly DyrepermenDbContext _db;
    private readonly IGjeldendeBruker _meg;
    private readonly ILogger<VedleggService> _log;

    public VedleggService(
        DyrepermenDbContext db, IGjeldendeBruker meg, ILogger<VedleggService> log)
    {
        _db = db;
        _meg = meg;
        _log = log;
    }

    public async Task<string?> Kontroller(
        IReadOnlyList<NyttVedlegg> filer, CancellationToken ct)
    {
        if (filer.Count == 0)
        {
            return null;
        }

        // Demoen deler database med ekte husstander. Tre hundre demoer med
        // vedlegg ville fylt den - og da stopper appen for alle.
        if (_meg.ErDemo)
        {
            return "Vedlegg kan ikke lastes opp i demoen.";
        }

        return Vedleggsregler.Feil(filer)
            ?? await Plassfeil(filer.Sum(f => (long)f.Data.Length), ct);
    }

    public async Task<Vedleggsresultat> LeggVedBesok(
        int besokId, IReadOnlyList<NyttVedlegg> filer, CancellationToken ct)
    {
        // Query-filteret er autorisasjonen: et besok i en annen husstand
        // finnes ikke herfra.
        var dyrId = await _db.Vetbesok
            .Where(v => v.Id == besokId)
            .Select(v => (int?)v.DyrId)
            .SingleOrDefaultAsync(ct);

        if (dyrId is null)
        {
            return Vedleggsresultat.BesoketFinnesIkke();
        }

        if (await Kontroller(filer, ct) is { } feil)
        {
            return Vedleggsresultat.Avvist(feil);
        }

        var idag = Tidssone.Idag(DateTimeOffset.UtcNow);

        foreach (var fil in filer)
        {
            _db.Dokument.Add(new Dokument
            {
                DyrId = dyrId.Value,
                VetbesokId = besokId,
                Originalnavn = Vedleggsregler.Navn(fil.Navn),
                // Kontroller har godkjent typen, sa den er aldri null her.
                Innholdstype = Vedleggsregler.Innholdstype(fil.Data)!,
                StorrelseByte = fil.Data.Length,
                Kategori = DokumentKategori.Kvittering,
                OpplastetDato = idag,
                Innhold = new DokumentInnhold { Data = fil.Data }
            });
        }

        // Alle filene i ett kall: enten kommer hele kvitteringen med, eller
        // ingen av sidene.
        await _db.SaveChangesAsync(ct);

        _log.LogInformation(
            "{Antall} vedlegg lagt ved besok {BesokId}", filer.Count, besokId);

        return Vedleggsresultat.Lagret();
    }

    public async Task<Vedleggsfil?> Hent(int dokumentId, CancellationToken ct)
        => await _db.DokumentInnhold
            .Where(i => i.DokumentId == dokumentId)
            .Select(i => new Vedleggsfil(
                i.Dokument.Originalnavn, i.Dokument.Innholdstype, i.Data))
            .SingleOrDefaultAsync(ct);

    public async Task<bool> Slett(int dokumentId, CancellationToken ct)
    {
        // Innholdet folger med ved kaskade.
        var slettet = await _db.Dokument
            .Where(d => d.Id == dokumentId)
            .ExecuteDeleteAsync(ct);

        return slettet > 0;
    }

    /// <summary>
    /// Er det plass til <paramref name="nyeByte"/> til? Forst husstandens
    /// eget tak, sa taket for hele databasen.
    ///
    /// Det siste summeres med IgnoreQueryFilters - bevisst, og bare som et
    /// tall. Ingen rader fra andre husstander leses, og meldingen sier ikke
    /// hvor mye andre har brukt.
    /// </summary>
    private async Task<string?> Plassfeil(long nyeByte, CancellationToken ct)
    {
        var husstandens = await _db.Dokument
            .SumAsync(d => (long)d.StorrelseByte, ct);

        if (husstandens + nyeByte > Vedleggsregler.MaksHusstandByte)
        {
            return "Det er ikke plass til flere vedlegg. Samlet kan vedleggene i "
                 + $"en husstand være høyst {Vedleggsregler.MaksHusstandByte / (1024 * 1024)} MB.";
        }

        var alle = await _db.Dokument
            .IgnoreQueryFilters()
            .SumAsync(d => (long)d.StorrelseByte, ct);

        if (alle + nyeByte > Vedleggsregler.MaksTotalByte)
        {
            _log.LogWarning("Taket for vedlegg i hele databasen er nadd");
            return "Det er ikke plass til flere vedlegg akkurat nå. Prøv igjen senere.";
        }

        return null;
    }
}
