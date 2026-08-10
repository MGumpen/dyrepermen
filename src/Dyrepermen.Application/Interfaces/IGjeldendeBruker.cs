namespace Dyrepermen.Application.Interfaces;

/// <summary>
/// Hvem som er innlogget, og hvilken husstand de ser pa. Fylles av
/// HusstandMiddleware i samme oppslag som setter HusstandId - det koster
/// altsa ingen ekstra sporring at sidemenyen viser navn og e-post.
///
/// Alternativet var claims, og det er nettopp det ADR 0001 forkastet:
/// claims blir foreldet nar noen endrer noe, og serveren kan ikke oppdatere
/// en annen brukers informasjonskapsel.
/// </summary>
public interface IGjeldendeBruker
{
    int? BrukerId { get; }

    string Visningsnavn { get; }

    string Epost { get; }

    string HusstandNavn { get; }

    bool ErInnlogget { get; }
}
