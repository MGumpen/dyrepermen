using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Infrastructure.Services;

public sealed class VeterinarService : IVeterinarService
{
    private readonly DyrepermenDbContext _db;
    private readonly IHusstandContext _husstand;

    public VeterinarService(DyrepermenDbContext db, IHusstandContext husstand)
    {
        _db = db;
        _husstand = husstand;
    }

    public async Task<IReadOnlyList<Veterinarrad>> Hent(CancellationToken ct)
    {
        var rader = await _db.Veterinar
            .OrderBy(v => v.Navn)
            .Select(v => new Veterinarrad(
                v.Id, v.Navn, v.Type, v.Telefon, v.Adresse, v.Nettside,
                v.Epost, v.Apningstider, v.Notat,
                // Korrelert undersporring - ingen ekstra rundtur per rad.
                v.Besok.Count))
            .ToListAsync(ct);

        // Typen sorterer, og den ma sorteres HER - ikke i SQL.
        //
        // OrderBy(v => v.Type) ser riktig ut, men HasConversion gjor at
        // databasen sorterer pa det lagrede TEGNET: 'A', 'F', 'S', 'V'. Da
        // havner Annet oeverst og vakta nest sist, stikk i strid med
        // enumrekkefolgen. Feilen er stille - listen kommer sortert, bare
        // feil sortert.
        //
        // En husstand har en handfull steder, sa sorteringen i minnet koster
        // ingenting.
        return rader.OrderBy(v => v.Type).ThenBy(v => v.Navn).ToList();
    }

    public async Task<Veterinarrad?> HentEn(int veterinarId, CancellationToken ct)
        => await _db.Veterinar
            .Where(v => v.Id == veterinarId)
            .Select(v => new Veterinarrad(
                v.Id, v.Navn, v.Type, v.Telefon, v.Adresse, v.Nettside,
                v.Epost, v.Apningstider, v.Notat, v.Besok.Count))
            .SingleOrDefaultAsync(ct);

    public async Task<bool> Opprett(NyVeterinar input, CancellationToken ct)
    {
        if (input.Navn.TomTilNull() is null)
        {
            return false;
        }

        _db.Veterinar.Add(new Veterinar
        {
            // Settes fra konteksten, ikke fra skjemaet. Kom den fra klienten,
            // kunne noen lagt en veterinaer inn i en fremmed husstand.
            HusstandId = _husstand.HusstandId,
            Navn = input.Navn.Trim(),
            Type = input.Type,
            Telefon = input.Telefon.TomTilNull(),
            Adresse = input.Adresse.TomTilNull(),
            Nettside = input.Nettside.TomTilNull(),
            Epost = input.Epost.TomTilNull(),
            Apningstider = input.Apningstider.TomTilNull(),
            Notat = input.Notat.TomTilNull(),
            OpprettetDato = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Oppdater(
        int veterinarId, NyVeterinar input, CancellationToken ct)
    {
        if (input.Navn.TomTilNull() is null)
        {
            return false;
        }

        // Query-filteret gjor eierskapssjekken: en veterinaer i en annen
        // husstand finnes ikke her.
        var rad = await _db.Veterinar
            .SingleOrDefaultAsync(v => v.Id == veterinarId, ct);

        if (rad is null)
        {
            return false;
        }

        rad.Navn = input.Navn.Trim();
        rad.Type = input.Type;
        rad.Telefon = input.Telefon.TomTilNull();
        rad.Adresse = input.Adresse.TomTilNull();
        rad.Nettside = input.Nettside.TomTilNull();
        rad.Epost = input.Epost.TomTilNull();
        rad.Apningstider = input.Apningstider.TomTilNull();
        rad.Notat = input.Notat.TomTilNull();

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Slett(int veterinarId, CancellationToken ct)
    {
        var rad = await _db.Veterinar
            .SingleOrDefaultAsync(v => v.Id == veterinarId, ct);

        if (rad is null)
        {
            return false;
        }

        // Besokene beholdes. Fremmednokkelen er satt til SetNull, sa
        // historikken star igjen med stedet tomt framfor a bli slettet.
        _db.Veterinar.Remove(rad);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<Vetbesokrad>> HentBesok(CancellationToken ct)
        => await _db.Vetbesok
            .OrderByDescending(x => x.Dato)
            .ThenByDescending(x => x.Id)
            .Select(x => new Vetbesokrad(
                x.Id,
                x.DyrId,
                x.Dyr.Navn,
                x.VeterinarId,
                x.Veterinar == null ? null : x.Veterinar.Navn,
                x.Klinikk,
                x.Dato,
                x.Klokkeslett,
                x.Arsak,
                x.Diagnose,
                x.KostnadKr,
                x.ForsikringKrevd,
                x.RefundertKr,
                x.NesteKontrollDato,
                x.Notat))
            .ToListAsync(ct);

    public async Task<bool> OpprettBesok(NyttVetbesok input, CancellationToken ct)
    {
        if (!await ErGyldig(input, ct))
        {
            return false;
        }

        _db.Vetbesok.Add(new Vetbesok
        {
            DyrId = input.DyrId,
            VeterinarId = input.VeterinarId,
            Klinikk = input.Klinikk.TomTilNull(),
            Dato = input.Dato,
            Klokkeslett = input.Klokkeslett,
            Arsak = input.Arsak.Trim(),
            Diagnose = input.Diagnose.TomTilNull(),
            KostnadKr = input.KostnadKr,
            ForsikringKrevd = input.ForsikringKrevd,
            // Uten krav gir refusjon ingen mening, og CHECK-vilkaret ville
            // avvist raden. Nulles her framfor a kaste.
            RefundertKr = input.ForsikringKrevd ? input.RefundertKr : null,
            NesteKontrollDato = input.NesteKontrollDato,
            Notat = input.Notat.TomTilNull()
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> OppdaterBesok(
        int besokId, NyttVetbesok input, CancellationToken ct)
    {
        if (!await ErGyldig(input, ct))
        {
            return false;
        }

        var rad = await _db.Vetbesok.SingleOrDefaultAsync(x => x.Id == besokId, ct);

        if (rad is null)
        {
            return false;
        }

        rad.DyrId = input.DyrId;
        rad.VeterinarId = input.VeterinarId;
        rad.Klinikk = input.Klinikk.TomTilNull();
        rad.Dato = input.Dato;
        rad.Klokkeslett = input.Klokkeslett;
        rad.Arsak = input.Arsak.Trim();
        rad.Diagnose = input.Diagnose.TomTilNull();
        rad.KostnadKr = input.KostnadKr;
        rad.ForsikringKrevd = input.ForsikringKrevd;
        rad.RefundertKr = input.ForsikringKrevd ? input.RefundertKr : null;
        rad.NesteKontrollDato = input.NesteKontrollDato;
        rad.Notat = input.Notat.TomTilNull();

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SlettBesok(int besokId, CancellationToken ct)
    {
        var rad = await _db.Vetbesok.SingleOrDefaultAsync(x => x.Id == besokId, ct);

        if (rad is null)
        {
            return false;
        }

        _db.Vetbesok.Remove(rad);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    /// <summary>
    /// Bade dyret og stedet ma tilhore husstanden. Query-filteret gjor
    /// jobben - en fremmed id finnes ikke i sporringen, og Any gir false.
    /// Uten sjekken pa VeterinarId kunne et besok pekt pa en annen husstands
    /// klinikk, og navnet lekket ut gjennom listen.
    /// </summary>
    private async Task<bool> ErGyldig(NyttVetbesok input, CancellationToken ct)
    {
        if (input.Arsak.TomTilNull() is null)
        {
            return false;
        }

        if (!await _db.Dyr.AnyAsync(d => d.Id == input.DyrId, ct))
        {
            return false;
        }

        return input.VeterinarId is null
            || await _db.Veterinar.AnyAsync(v => v.Id == input.VeterinarId, ct);
    }
}
