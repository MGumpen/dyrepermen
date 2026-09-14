using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IKontoService
{
    /// <summary>
    /// Alle data husstanden eier, som JSON. Tilbys for sletting - kostnaden
    /// er lav, angrefristen er null.
    /// </summary>
    Task<string> EksporterJson(int brukerId, CancellationToken ct);

    /// <summary>
    /// Sletter brukerraden og avidentifiserer alt vedkommende har
    /// registrert. Personopplysninger slettes; husstandens data bestar.
    ///
    /// En kaskadesletting fra brukeren ville tatt med seg hele
    /// vekthistorikken til hunden fordi det tilfeldigvis var denne personen
    /// som registrerte malingene. Se plan kapittel 12.5.
    /// </summary>
    Task<SlettResultat> SlettBruker(
        int brukerId,
        string passord,
        bool bekreftetSletteHusstand,
        CancellationToken ct);

    /// <summary>Antall dyr i husstanden, for varselet til siste medlem.</summary>
    Task<(bool ErSisteMedlem, int AntallDyr)> Slettekonsekvens(
        int brukerId, CancellationToken ct);
}
