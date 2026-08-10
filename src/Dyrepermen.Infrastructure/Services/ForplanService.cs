using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Infrastructure.Services;

/// <summary>
/// Applikasjonen anbefaler ikke formengde. Den regner ut regelen brukeren
/// selv har lagt inn. Riktig mengde avhenger av art, rase, alder, fortype,
/// aktivitetsniva og hold - en innebygd formel ville gitt et tall som ser
/// autoritativt ut uten a ha dekning for det. Se plan kapittel 8.1.
/// </summary>
public sealed class ForplanService : IForplanService
{
    private readonly DyrepermenDbContext _db;

    public ForplanService(DyrepermenDbContext db) => _db = db;

    public async Task<ForplanResultat> BeregnAktiv(int dyrId, CancellationToken ct)
    {
        var plan = await _db.Forplan
            .Where(f => f.DyrId == dyrId && f.Aktiv)
            .Select(f => new
            {
                f.Metode,
                f.ProsentTidels,
                f.GramPerDag,
                f.AntallMaltider
            })
            .SingleOrDefaultAsync(ct);

        if (plan is null)
        {
            return ForplanResultat.IngenPlan();
        }

        if (plan.Metode == Formetode.Gram)
        {
            // Fast mengde star stille til den endres.
            return ForplanResultat.Ok(plan.GramPerDag!.Value, plan.AntallMaltider);
        }

        // Prosentmetoden er levende: den leser siste vektregistrering hver
        // gang, sa mengden folger valpen gjennom vekstfasen uten at noen ma
        // huske a justere.
        var siste = await _db.Vekt
            .Where(v => v.DyrId == dyrId)
            .OrderByDescending(v => v.Dato)
            .ThenByDescending(v => v.Id)
            .Select(v => new { v.VektGram, v.Dato })
            .FirstOrDefaultAsync(ct);

        if (siste is null)
        {
            // IKKE 0 gram. Uten vektgrunnlag har tallet ingen mening, og et
            // tall uten dekning er verre enn ingen tall.
            return ForplanResultat.ManglerVektgrunnlag();
        }

        // prosent_tidels = 50 betyr 5,0 %.
        var gramPerDag = (int)Math.Round(
            siste.VektGram * plan.ProsentTidels!.Value / 1000.0,
            MidpointRounding.AwayFromZero);

        return ForplanResultat.Ok(
            gramPerDag,
            plan.AntallMaltider,
            grunnlagVektGram: siste.VektGram,
            grunnlagDato: siste.Dato);
    }

    public async Task<ForplanRad?> HentAktiv(int dyrId, CancellationToken ct)
        => await _db.Forplan
            .Where(f => f.DyrId == dyrId && f.Aktiv)
            .Select(f => new ForplanRad(
                f.Id, f.Metode, f.ProsentTidels, f.GramPerDag,
                f.AntallMaltider, f.Fornavn, f.Notat, f.OpprettetDato))
            .SingleOrDefaultAsync(ct);

    public async Task<bool> Opprett(NyForplan input, CancellationToken ct)
    {
        if (!await _db.Dyr.AnyAsync(d => d.Id == input.DyrId, ct))
        {
            return false;
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Gammel plan beholdes med aktiv = false. ux_forplan_aktiv tillater
        // kun en aktiv per dyr, sa deaktiveringen ma skje for innsettingen -
        // og i samme transaksjon, ellers star dyret uten plan om noe feiler.
        var gjeldende = await _db.Forplan
            .Where(f => f.DyrId == input.DyrId && f.Aktiv)
            .ToListAsync(ct);

        foreach (var gammel in gjeldende)
        {
            gammel.Aktiv = false;
        }

        await _db.SaveChangesAsync(ct);

        // Feltet som ikke hoerer til valgt metode nulles ut. Databasen skal
        // ikke kunne inneholde en plan som er halvt prosentbasert og halvt
        // fast - ck_forplan_verdi handhever det, og her unngar vi a bryte den.
        var erProsent = input.Metode == Formetode.Prosent;

        _db.Forplan.Add(new Forplan
        {
            DyrId = input.DyrId,
            Metode = input.Metode,
            ProsentTidels = erProsent ? input.ProsentTidels : null,
            GramPerDag = erProsent ? null : input.GramPerDag,
            AntallMaltider = input.AntallMaltider,
            Fornavn = input.Fornavn.TomTilNull(),
            Notat = input.Notat.TomTilNull(),
            Aktiv = true
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<bool> Deaktiver(int dyrId, CancellationToken ct)
    {
        var plan = await _db.Forplan
            .SingleOrDefaultAsync(f => f.DyrId == dyrId && f.Aktiv, ct);

        if (plan is null)
        {
            return false;
        }

        // Slettes ikke. Gamle planer er revisjonsspor.
        plan.Aktiv = false;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
