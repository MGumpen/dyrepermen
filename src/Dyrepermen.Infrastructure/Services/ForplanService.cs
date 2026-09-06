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
///
/// Selve regnestykket ligger i <see cref="Forberegning"/>. Tjenesten henter
/// grunnlaget og overlater regelen til den, sa forplansiden, dashbordet og
/// utskriften ikke kan komme til hvert sitt svar.
/// </summary>
public sealed class ForplanService : IForplanService
{
    private readonly DyrepermenDbContext _db;

    public ForplanService(DyrepermenDbContext db) => _db = db;

    public async Task<ForplanResultat> BeregnAktiv(int dyrId, CancellationToken ct)
    {
        // Fodselsdato, siste vekt og planen i samme rundtur. Undersporringene
        // blir til LEFT JOIN LATERAL - ikke tre kall.
        var rad = await _db.Dyr
            .Where(d => d.Id == dyrId)
            .Select(d => new
            {
                d.Fodselsdato,

                SisteVekt = d.Vekter
                    .OrderByDescending(v => v.Dato)
                    .ThenByDescending(v => v.Id)
                    .Select(v => new { v.VektGram, v.Dato })
                    .FirstOrDefault(),

                Plan = d.Forplaner
                    .Where(f => f.Aktiv)
                    .Select(f => new
                    {
                        f.Id,
                        f.Metode,
                        f.ProsentTidels,
                        f.GramPerDag,
                        f.AntallMaltider,
                        f.VektdelAndelProsent
                    })
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync(ct);

        if (rad?.Plan is not { } plan)
        {
            return ForplanResultat.IngenPlan();
        }

        var regel = new Forplanregel(
            plan.Metode,
            plan.ProsentTidels,
            plan.GramPerDag,
            plan.AntallMaltider,
            plan.VektdelAndelProsent,
            await HentTrinn(plan.Metode, plan.Id, ct));

        return Forberegning.Beregn(regel, new Beregningsgrunnlag(
            rad.SisteVekt?.VektGram,
            rad.SisteVekt?.Dato,
            rad.Fodselsdato,
            Tidssone.Idag(DateTimeOffset.UtcNow)));
    }

    public async Task<ForplanRad?> HentAktiv(int dyrId, CancellationToken ct)
    {
        var plan = await _db.Forplan
            .Where(f => f.DyrId == dyrId && f.Aktiv)
            .Select(f => new ForplanRad(
                f.Id, f.Metode, f.ProsentTidels, f.GramPerDag,
                f.AntallMaltider, f.Fornavn, f.Notat, f.OpprettetDato,
                f.EndretDato,
                // Alle argumentene skrives ut. Et uttrykkstre tar ikke imot
                // valgfrie parametere, og trinnene hentes uansett for seg.
                f.VektdelAndelProsent, f.FornavnAlder, null))
            .SingleOrDefaultAsync(ct);

        if (plan is null)
        {
            return null;
        }

        return plan with { Tabelltrinn = await HentTrinn(plan.Metode, plan.Id, ct) };
    }

    public async Task<bool> Opprett(Forplaninnhold input, CancellationToken ct)
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

        // Feltene som ikke hoerer til valgt metode nulles ut. Databasen skal
        // ikke kunne inneholde en plan som er halvt prosentbasert og halvt
        // fast - ck_forplan_verdi handhever det, og her unngar vi a bryte den.
        var plan = new Forplan
        {
            DyrId = input.DyrId,
            Metode = input.Metode,
            ProsentTidels = Prosentsats(input),
            GramPerDag = input.Metode == Formetode.Gram ? input.GramPerDag : null,
            VektdelAndelProsent = input.Metode == Formetode.Tabell
                ? input.VektdelAndelProsent ?? 0
                : null,
            AntallMaltider = input.AntallMaltider,
            Fornavn = input.Fornavn.TomTilNull(),
            FornavnAlder = input.Metode == Formetode.Tabell
                ? input.FornavnAlder.TomTilNull()
                : null,
            Notat = input.Notat.TomTilNull(),
            Aktiv = true
        };

        if (input.Metode == Formetode.Tabell)
        {
            foreach (var trinn in (input.Tabelltrinn ?? []).OrderBy(t => t.AlderMnd))
            {
                plan.Tabelltrinn.Add(new Forplantrinn
                {
                    AlderMnd = trinn.AlderMnd,
                    GramPerDag = trinn.GramPerDag
                });
            }
        }

        _db.Forplan.Add(plan);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<bool> Oppdater(Forplaninnhold input, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Query-filteret er autorisasjonen: en plan i en annen husstand
        // finnes ikke herfra, og controlleren gjor null om til 404.
        var plan = await _db.Forplan
            .Include(f => f.Tabelltrinn)
            .SingleOrDefaultAsync(f => f.DyrId == input.DyrId && f.Aktiv, ct);

        if (plan is null)
        {
            return false;
        }

        var erTabell = input.Metode == Formetode.Tabell;

        plan.Metode = input.Metode;
        plan.ProsentTidels = Prosentsats(input);
        plan.GramPerDag = input.Metode == Formetode.Gram ? input.GramPerDag : null;
        plan.VektdelAndelProsent = erTabell ? input.VektdelAndelProsent ?? 0 : null;
        plan.AntallMaltider = input.AntallMaltider;
        plan.Fornavn = input.Fornavn.TomTilNull();
        plan.FornavnAlder = erTabell ? input.FornavnAlder.TomTilNull() : null;
        plan.Notat = input.Notat.TomTilNull();
        plan.EndretDato = Tidssone.Idag(DateTimeOffset.UtcNow);

        // Tabellen erstattes i sin helhet. A finne ut hvilke rader som er
        // endret, lagt til og fjernet ville vaert tre spesialtilfeller der
        // ett hold.
        //
        // Slettingen lagres FOR innsettingen. Skjer begge i samme
        // SaveChanges, bestemmer EF rekkefolgen - og en ny rad pa samme
        // maned som en gammel bryter da ux_forplantrinn_alder.
        _db.Forplantrinn.RemoveRange(plan.Tabelltrinn.ToList());
        await _db.SaveChangesAsync(ct);

        if (erTabell)
        {
            foreach (var trinn in (input.Tabelltrinn ?? []).OrderBy(t => t.AlderMnd))
            {
                _db.Forplantrinn.Add(new Forplantrinn
                {
                    ForplanId = plan.Id,
                    AlderMnd = trinn.AlderMnd,
                    GramPerDag = trinn.GramPerDag
                });
            }
        }

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

        // Slettes ikke. Gamle planer er revisjonsspor, og torrfortabellen
        // folger planen sin.
        plan.Aktiv = false;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<string>> HentFornavn(CancellationToken ct)
    {
        // Query-filteret gjor dette til husstandens egne navn. Uten det
        // ville forslagslisten lekket hva naboen forer med.
        //
        // Duplikatene fjernes i C#, ikke i SQL: rekkefolgen er "nyeste
        // forst", og et DISTINCT over to kolonner mister den. Utvalget er
        // husstandens egne planer, altsa titalls rader.
        var fraPlaner = await _db.Forplan
            .OrderByDescending(f => f.OpprettetDato)
            .ThenByDescending(f => f.Id)
            .Select(f => new { f.Fornavn, f.FornavnAlder })
            .ToListAsync(ct);

        // Loggen ogsa. Der skrives navnet oftest forste gang - lenge for
        // noen legger det inn i en plan.
        var fraLoggen = await _db.Foring
            .Where(f => f.Type == Foringstype.Maltid && f.Fornavn != null)
            .GroupBy(f => f.Fornavn!)
            .OrderByDescending(g => g.Max(f => f.Tidspunkt))
            .Select(g => g.Key)
            .Take(15)
            .ToListAsync(ct);

        return
        [
            .. fraPlaner
                .SelectMany(n => new[] { n.Fornavn, n.FornavnAlder })
                .Concat(fraLoggen)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                // En liste med hundre forslag er ingen hjelp. Tjue dekker
                // det en husstand faktisk har brukt.
                .Take(20)
        ];
    }

    /// <summary>
    /// Prosentsatsen hoerer til prosentmetoden, og til tabellmetoden nar den
    /// blander inn rafor. En ren tabellplan har ingen - ck_forplan_verdi
    /// krever at den er null, nettopp sa det ikke blir liggende igjen et tall
    /// som ser ut som en regel uten a vaere det.
    /// </summary>
    private static int? Prosentsats(Forplaninnhold input) => input.Metode switch
    {
        Formetode.Prosent => input.ProsentTidels,
        Formetode.Tabell when input.VektdelAndelProsent > 0 => input.ProsentTidels,
        _ => null
    };

    /// <summary>
    /// Egen rundtur, og kun for tabellplaner. En kolleksjon inne i en
    /// projeksjon som allerede ligger i en undersporring, er mer enn
    /// oversetteren bor be om - og planer uten torrfor skal ikke betale for
    /// en tabell de ikke har.
    /// </summary>
    private async Task<IReadOnlyList<Alderstrinn>> HentTrinn(
        Formetode metode, int forplanId, CancellationToken ct)
        => metode != Formetode.Tabell
            ? []
            : await _db.Forplantrinn
                .Where(t => t.ForplanId == forplanId)
                .OrderBy(t => t.AlderMnd)
                .Select(t => new Alderstrinn(t.AlderMnd, t.GramPerDag))
                .ToListAsync(ct);
}
