using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Interfaces;

public interface IForplanService
{
    /// <summary>
    /// Regner ut den aktive planen. Prosentmetoden leser siste
    /// vektregistrering hver gang, sa mengden folger valpen automatisk
    /// gjennom vekstfasen. Se plan kapittel 8.1.
    /// </summary>
    Task<ForplanResultat> BeregnAktiv(int dyrId, CancellationToken ct);

    Task<ForplanRad?> HentAktiv(int dyrId, CancellationToken ct);

    /// <summary>
    /// Deaktiverer eventuell eksisterende plan og oppretter den nye i samme
    /// transaksjon. ux_forplan_aktiv tillater kun en aktiv plan per dyr.
    /// False betyr at dyret ikke finnes i denne husstanden.
    /// </summary>
    Task<bool> Opprett(NyForplan input, CancellationToken ct);

    Task<bool> Deaktiver(int dyrId, CancellationToken ct);
}
