using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dyrepermen.Infrastructure.Services;

/// <summary>
/// Merk at fornyelse av innloggingskapselen IKKE skjer her.
/// <c>SignInManager</c> bor i ASP.NET Core-rammeverket, og a dra hele
/// webrammeverket inn i datalaget for ett kall er feil vei. Controlleren
/// kaller <c>RefreshSignInAsync</c> etter at denne har returnert.
///
/// Det er trygt fordi husstand leses fra database, ikke fra claim (ADR 0001).
/// Fornyelsen er en forutsigbarhetsgaranti, ikke et krav for at filtrene skal
/// virke - se plan kapittel 12.3.1.
/// </summary>
public sealed class HusstandService : IHusstandService
{
    private readonly DyrepermenDbContext _db;
    private readonly ILogger<HusstandService> _log;

    public HusstandService(DyrepermenDbContext db, ILogger<HusstandService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<int> OpprettHusstand(
        string navn, int brukerId, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var husstand = new Husstand { Navn = navn };
        _db.Husstand.Add(husstand);
        await _db.SaveChangesAsync(ct);

        // Ma opprettes i samme transaksjon. Mangler raden, faller OpprettDyr
        // tilbake pa hardkodede standardverdier og innstillingssiden krasjer
        // pa en null-referanse. Plan kapittel 12.2.
        _db.HusstandInnstilling.Add(new HusstandInnstilling
        {
            HusstandId = husstand.Id
        });

        var bruker = await _db.Users.SingleAsync(u => u.Id == brukerId, ct);
        bruker.HusstandId = husstand.Id;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        _log.LogInformation(
            "Husstand {HusstandId} opprettet av {BrukerId}",
            husstand.Id, brukerId);

        return husstand.Id;
    }
}
