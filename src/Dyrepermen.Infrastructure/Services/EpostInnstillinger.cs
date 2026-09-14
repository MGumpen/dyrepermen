namespace Dyrepermen.Infrastructure.Services;

/// <summary>
/// SMTP-oppsettet fra seksjonen "Epost". Lokalt fra user-secrets, i
/// produksjon fra miljovariablene Epost__SmtpHost og videre. Aldri fra
/// appsettings. Se ADR 0014.
/// </summary>
public sealed class EpostInnstillinger
{
    public const string Seksjon = "Epost";

    public string? SmtpHost { get; set; }

    /// <summary>587 med STARTTLS. Port 465 (implisitt TLS) stottes ikke av SmtpClient.</summary>
    public int Port { get; set; } = 587;

    public string? Bruker { get; set; }

    public string? Passord { get; set; }

    /// <summary>
    /// Fra-adressen. Egen innstilling, ikke Bruker: hos flere SMTP-tjenester
    /// er brukernavnet "apikey" eller lignende, ikke en adresse.
    /// </summary>
    public string? Avsender { get; set; }

    /// <summary>Hvor henvendelsene fra kontaktskjemaet sendes.</summary>
    public string? Kontaktmottaker { get; set; }

    /// <summary>
    /// Mangler en av delene, sendes ingenting. Skjemaet sier da fra til
    /// brukeren i stedet for a late som meldingen gikk.
    /// </summary>
    public bool ErSattOpp =>
        !string.IsNullOrWhiteSpace(SmtpHost)
        && !string.IsNullOrWhiteSpace(Bruker)
        && !string.IsNullOrWhiteSpace(Passord)
        && !string.IsNullOrWhiteSpace(Avsender)
        && !string.IsNullOrWhiteSpace(Kontaktmottaker);
}
