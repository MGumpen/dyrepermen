using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dyrepermen.Infrastructure.Services;

public sealed class MedisinService : IMedisinService
{
    private static readonly TimeZoneInfo Oslo =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");

    private readonly DyrepermenDbContext _db;
    private readonly ILogger<MedisinService> _log;

    public MedisinService(DyrepermenDbContext db, ILogger<MedisinService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IReadOnlyList<MedisinRad>> HentFor(
        int dyrId, CancellationToken ct)
        => await _db.Medisin
            .Where(m => m.DyrId == dyrId)
            // Avsluttede nederst. Sluttdato alene er ikke nok - en kur med
            // planlagt slutt kan godt vaere i gang.
            .OrderBy(m => m.AvsluttetTid != null)
            .ThenBy(m => m.SluttDato != null)
            .ThenByDescending(m => m.StartDato)
            .Select(m => new MedisinRad(
                m.Id,
                m.Navn,
                m.Dose,
                m.IntervallTimer,
                m.StartDato,
                m.SluttDato,
                // Korrelert undersporring - siste dose hentes i samme rundtur.
                m.Doser
                    .OrderByDescending(d => d.GittTid)
                    .Select(d => (DateTimeOffset?)d.GittTid)
                    .FirstOrDefault(),
                m.Doser
                    .OrderByDescending(d => d.GittTid)
                    .Select(d => d.GittAv == null ? null : d.GittAv.Visningsnavn)
                    .FirstOrDefault(),
                m.AvsluttetTid))
            .ToListAsync(ct);

    public async Task<bool> Registrer(NyMedisin input, CancellationToken ct)
    {
        if (!await _db.Dyr.AnyAsync(d => d.Id == input.DyrId, ct))
        {
            return false;
        }

        _db.Medisin.Add(new Medisin
        {
            DyrId = input.DyrId,
            Navn = input.Navn.Trim(),
            Dose = input.Dose.Trim(),
            IntervallTimer = input.IntervallTimer,
            StartDato = input.StartDato,
            SluttDato = input.SluttDato
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Avslutt(int dyrId, int medisinId, CancellationToken ct)
    {
        var medisin = await _db.Medisin
            .SingleOrDefaultAsync(m => m.Id == medisinId && m.DyrId == dyrId, ct);

        if (medisin is null)
        {
            return false;
        }

        // Avsluttet na, ikke ved midnatt. Sluttdatoen settes til i dag nar
        // den mangler eller ligger lenger fram, sa perioden viser nar kuren
        // faktisk sluttet. En sluttdato som allerede har passert, beholdes.
        var naa = DateTimeOffset.UtcNow;
        var idag = Tidssone.Idag(naa);

        medisin.AvsluttetTid ??= naa;

        if (medisin.SluttDato is null || medisin.SluttDato > idag)
        {
            medisin.SluttDato = idag;
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> Oppdater(
        int dyrId, int medisinId, NyMedisin input, CancellationToken ct)
    {
        // Query-filteret er autorisasjonen, DyrId hindrer at en id fra et
        // annet dyr i egen husstand treffer.
        var medisin = await _db.Medisin
            .SingleOrDefaultAsync(m => m.Id == medisinId && m.DyrId == dyrId, ct);

        if (medisin is null)
        {
            return false;
        }

        // Doseloggen rores ikke. Den sier nar noe ble gitt og av hvem, og
        // det er like sant etter at dosen er trappet ned.
        medisin.Navn = input.Navn.Trim();
        medisin.Dose = input.Dose.Trim();
        medisin.IntervallTimer = input.IntervallTimer;
        medisin.StartDato = input.StartDato;
        medisin.SluttDato = input.SluttDato;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<DoseResultat> LoggDose(
        int dyrId,
        int medisinId,
        int? brukerId,
        bool bekreftet,
        CancellationToken ct)
    {
        var medisin = await _db.Medisin
            .SingleOrDefaultAsync(m => m.Id == medisinId && m.DyrId == dyrId, ct);

        // En avsluttet medisin skal ikke gis. Knappen er skjult, men en fane
        // som har statt apen, kan fortsatt poste.
        if (medisin is null || medisin.AvsluttetTid is not null)
        {
            return DoseResultat.FinnesIkke();
        }

        var na = DateTimeOffset.UtcNow;

        // Intervall 0 betyr ved behov - da finnes ingen for tidlig.
        if (!bekreftet && medisin.IntervallTimer > 0)
        {
            var siste = await _db.Dose
                .Where(d => d.MedisinId == medisinId)
                .OrderByDescending(d => d.GittTid)
                .Select(d => new
                {
                    d.GittTid,
                    Navn = d.GittAv == null ? null : d.GittAv.Visningsnavn
                })
                .FirstOrDefaultAsync(ct);

            if (siste is not null)
            {
                var tidligst = siste.GittTid.AddHours(medisin.IntervallTimer);

                if (na < tidligst)
                {
                    _log.LogWarning(
                        "Dose for medisin {MedisinId} forsokt gitt for tidlig",
                        medisinId);

                    return DoseResultat.ForTidlig(Melding(siste.GittTid, siste.Navn, tidligst));
                }
            }
        }

        _db.Dose.Add(new Dose
        {
            MedisinId = medisinId,
            // Tidspunktet settes pa serveren, aldri fra klienten.
            GittTid = na,
            GittAvBrukerId = brukerId
        });

        await _db.SaveChangesAsync(ct);
        _log.LogInformation("Dose logget for medisin {MedisinId}", medisinId);

        return DoseResultat.Lagret();
    }

    /// <summary>
    /// Tidspunkter lagres i UTC og konverteres til Europe/Oslo her. Uten
    /// konverteringen forskyver alt seg en time ved sommertidsomstillingen.
    /// </summary>
    private static string Melding(
        DateTimeOffset sisteDose, string? gittAv, DateTimeOffset tidligst)
    {
        var lokal = TimeZoneInfo.ConvertTime(sisteDose, Oslo);
        var neste = TimeZoneInfo.ConvertTime(tidligst, Oslo);
        var hvem = gittAv is null ? "" : $" av {gittAv}";

        return $"Forrige dose ble gitt {lokal:HH:mm}{hvem}. "
             + $"Neste dose kan tidligst gis {neste:HH:mm}.";
    }
}
