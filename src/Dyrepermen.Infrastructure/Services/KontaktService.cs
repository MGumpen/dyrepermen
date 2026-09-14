using System.Net;
using System.Net.Mail;
using System.Text;
using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dyrepermen.Infrastructure.Services;

/// <summary>
/// Sender henvendelser fra kontaktskjemaet over SMTP. Ingenting lagres i
/// databasen. Se ADR 0014.
/// </summary>
public sealed class KontaktService : IKontaktService
{
    private readonly EpostInnstillinger _epost;
    private readonly IGjeldendeBruker _bruker;
    private readonly ILogger<KontaktService> _log;

    public KontaktService(
        IOptions<EpostInnstillinger> epost,
        IGjeldendeBruker bruker,
        ILogger<KontaktService> log)
    {
        _epost = epost.Value;
        _bruker = bruker;
        _log = log;
    }

    public async Task<Kontaktresultat> Send(
        Kontakttype type, string melding, CancellationToken ct)
    {
        // FallbackPolicy sorger for innlogging. Er BrukerId likevel tom, har
        // HusstandMiddleware ikke kjort - da er noe galt i oppsettet, ikke
        // hos brukeren.
        var brukerId = _bruker.BrukerId
            ?? throw new InvalidOperationException(
                "Kontaktskjemaet krever en innlogget bruker.");

        if (!_epost.ErSattOpp)
        {
            _log.LogWarning(
                "Henvendelse fra bruker {BrukerId} ble ikke sendt: e-post er ikke satt opp",
                brukerId);
            return Kontaktresultat.IkkeSattOpp;
        }

        using var epost = new MailMessage
        {
            From = new MailAddress(_epost.Avsender!, "Dyrepermen"),
            Subject = Kontaktepost.Emne(type),
            SubjectEncoding = Encoding.UTF8,
            Body = Kontaktepost.Tekst(
                type, melding, brukerId, _bruker.Visningsnavn, _bruker.Epost,
                DateTimeOffset.UtcNow),
            BodyEncoding = Encoding.UTF8,
            // Ren tekst. Som HTML ville meldingen kunnet inneholde lenker og
            // markup som ser ut som de kommer fra appen.
            IsBodyHtml = false
        };
        epost.To.Add(_epost.Kontaktmottaker!);

        // Svar-til er brukeren, sa et svar fra innboksen gar rett til henne.
        if (!string.IsNullOrWhiteSpace(_bruker.Epost))
        {
            epost.ReplyToList.Add(new MailAddress(_bruker.Epost, _bruker.Visningsnavn));
        }

        using var smtp = new SmtpClient(_epost.SmtpHost, _epost.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_epost.Bruker, _epost.Passord),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            // Standard er 100 sekunder. Sa lenge skal ingen sta og vente pa
            // et skjema.
            Timeout = 15_000
        };

        try
        {
            await smtp.SendMailAsync(epost, ct);
        }
        catch (SmtpException feil)
        {
            // Unntaket logges IKKE i sin helhet. Serverens svar gjengir ofte
            // adressen det gjelder, og e-postadresser skal aldri i loggen.
            _log.LogError(
                "Henvendelse fra bruker {BrukerId} kunne ikke sendes. SMTP-status: {Status}",
                brukerId, feil.StatusCode);
            return Kontaktresultat.Feilet;
        }

        _log.LogInformation(
            "Henvendelse ({Type}) sendt fra bruker {BrukerId}", type, brukerId);
        return Kontaktresultat.Sendt;
    }
}
