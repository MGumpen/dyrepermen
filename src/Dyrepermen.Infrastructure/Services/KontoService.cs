using System.Text.Json;
using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dyrepermen.Infrastructure.Services;

public sealed class KontoService : IKontoService
{
    private static readonly JsonSerializerOptions Format = new()
    {
        WriteIndented = true,
        // Uten denne blir casingen blandet: nokler jeg navngir selv blir sma,
        // mens egenskaper fra entitetene beholder PascalCase. Et eksportformat
        // skal vaere konsistent - noen skal kunne lese det om fem ar.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Norske tegn skal staa som seg selv, ikke som å.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder
            .UnsafeRelaxedJsonEscaping
    };

    private readonly DyrepermenDbContext _db;
    private readonly IHusstandContext _husstand;
    private readonly UserManager<Bruker> _brukere;
    private readonly ILogger<KontoService> _log;

    public KontoService(
        DyrepermenDbContext db,
        IHusstandContext husstand,
        UserManager<Bruker> brukere,
        ILogger<KontoService> log)
    {
        _db = db;
        _husstand = husstand;
        _brukere = brukere;
        _log = log;
    }

    public async Task<string> EksporterJson(int brukerId, CancellationToken ct)
    {
        var husstandId = _husstand.HusstandId;

        var data = new
        {
            eksportert = DateTimeOffset.UtcNow,
            husstand = await _db.Husstand
                .Where(h => h.Id == husstandId)
                .Select(h => new { h.Navn, h.OpprettetDato })
                .SingleOrDefaultAsync(ct),

            medlemmer = await _db.Users
                .Where(u => u.HusstandId == husstandId)
                .Select(u => new { u.Visningsnavn, u.Email })
                .ToListAsync(ct),

            // IgnoreQueryFilters pa Dyr: eksporten skal ogsa inneholde
            // deaktiverte dyr. Historikken om et dyr som er gatt bort er
            // nettopp det brukeren vil ha med seg.
            dyr = await _db.Dyr
                .IgnoreQueryFilters()
                .Where(d => d.HusstandId == husstandId)
                .Select(d => new
                {
                    d.Navn,
                    Art = d.Art.ToString(),
                    Kjonn = d.Kjonn.ToString(),
                    d.Rase,
                    d.Fodselsdato,
                    d.ChipNr,
                    d.RegNrNkk,
                    d.Kastrert,
                    d.Aktiv,
                    vekter = d.Vekter
                        .OrderBy(v => v.Dato)
                        .Select(v => new { v.Dato, v.VektGram }),
                    behandlinger = d.Behandlinger
                        .OrderBy(b => b.Dato)
                        .Select(b => new
                        {
                            Type = b.Type.ToString(),
                            b.Preparat,
                            b.Dato,
                            b.NesteDato,
                            b.Notat
                        }),
                    medisiner = d.Medisiner
                        .OrderBy(m => m.StartDato)
                        .Select(m => new
                        {
                            m.Navn,
                            m.Dose,
                            m.IntervallTimer,
                            m.StartDato,
                            m.SluttDato,
                            doser = m.Doser.OrderBy(x => x.GittTid).Select(x => x.GittTid)
                        }),
                    forplaner = d.Forplaner
                        .OrderBy(f => f.OpprettetDato)
                        .Select(f => new
                        {
                            Metode = f.Metode.ToString(),
                            f.ProsentTidels,
                            f.GramPerDag,
                            f.AntallMaltider,
                            f.Fornavn,
                            f.Notat,
                            f.Aktiv,
                            f.OpprettetDato
                        })
                })
                .ToListAsync(ct)
        };

        _log.LogInformation("Dataeksport kjort av {BrukerId}", brukerId);

        return JsonSerializer.Serialize(data, Format);
    }

    public async Task<(bool ErSisteMedlem, int AntallDyr)> Slettekonsekvens(
        int brukerId, CancellationToken ct)
    {
        var husstandId = await _db.Users
            .Where(u => u.Id == brukerId)
            .Select(u => u.HusstandId)
            .SingleOrDefaultAsync(ct);

        if (husstandId is null)
        {
            return (false, 0);
        }

        var medlemmer = await _db.Users
            .CountAsync(u => u.HusstandId == husstandId, ct);

        var dyr = await _db.Dyr
            .IgnoreQueryFilters()
            .CountAsync(d => d.HusstandId == husstandId, ct);

        return (medlemmer == 1, dyr);
    }

    public async Task<SlettResultat> SlettBruker(
        int brukerId,
        string passord,
        bool bekreftetSletteHusstand,
        CancellationToken ct)
    {
        var bruker = await _db.Users.SingleAsync(u => u.Id == brukerId, ct);

        // Enheten kan sta ulast. Vedvarende innlogging i 30 dager gjor dette
        // viktigere, ikke mindre viktig. Se plan kapittel 12.5.
        if (!await _brukere.CheckPasswordAsync(bruker, passord))
        {
            return SlettResultat.FeilPassord;
        }

        var husstandId = bruker.HusstandId;

        var sisteMedlem = husstandId is not null
            && await _db.Users.CountAsync(u => u.HusstandId == husstandId, ct) == 1;

        if (sisteMedlem && !bekreftetSletteHusstand)
        {
            return SlettResultat.MaBekrefteHusstandsletting;
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // ON DELETE SET NULL i skjemaet gjor avidentifiseringen: vektrader,
        // doser og foringer beholdes med null i *_av_bruker_id. Visningslaget
        // skriver "slettet bruker" der navnet sto.
        //
        // En kaskadesletting ville tatt med seg hele vekthistorikken til
        // hunden fordi det tilfeldigvis var denne personen som registrerte
        // malingene. Det er feil, og det er ikke til a reversere.
        var resultat = await _brukere.DeleteAsync(bruker);
        if (!resultat.Succeeded)
        {
            await tx.RollbackAsync(ct);
            return SlettResultat.FeilPassord;
        }

        if (sisteMedlem)
        {
            await _db.Dyr.IgnoreQueryFilters()
                .Where(d => d.HusstandId == husstandId)
                .ExecuteDeleteAsync(ct);

            await _db.Handleliste.IgnoreQueryFilters()
                .Where(x => x.HusstandId == husstandId)
                .ExecuteDeleteAsync(ct);

            await _db.Husstand
                .Where(h => h.Id == husstandId)
                .ExecuteDeleteAsync(ct);
        }

        await tx.CommitAsync(ct);

        // Logg bruker-ID, aldri e-postadressen.
        _log.LogInformation(
            "Bruker {BrukerId} slettet permanent. Husstand slettet: {Husstand}",
            brukerId, sisteMedlem);

        return SlettResultat.Ok;
    }
}
