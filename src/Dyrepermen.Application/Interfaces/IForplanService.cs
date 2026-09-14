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
    Task<bool> Opprett(Forplaninnhold input, CancellationToken ct);

    /// <summary>
    /// Endrer den aktive planen i stedet for a erstatte den.
    ///
    /// En liten justering - to maltider i stedet for tre, eller andelen
    /// rafor flyttet fra 70 til 60 - er ikke en ny plan, og skal ikke fylle
    /// historikken med en rad per uke. Se ADR 0013.
    ///
    /// Torrfortabellen erstattes i sin helhet. False betyr at dyret ikke
    /// finnes i denne husstanden, eller at det ikke har en aktiv plan a
    /// endre.
    /// </summary>
    Task<bool> Oppdater(Forplaninnhold input, CancellationToken ct);

    Task<bool> Deaktiver(int dyrId, CancellationToken ct);

    /// <summary>
    /// Fornavnene husstanden har brukt for, nyeste forst.
    ///
    /// "VOM Puppy" skal skrives inn en gang, ikke en gang per dyr og en gang
    /// per plan. Historikken er allerede der - den er den eneste kilden som
    /// vet hva denne husstanden faktisk forer med.
    ///
    /// Bade tidligere planer og foringsloggen. Loggen er der navnet oftest
    /// skrives forste gang, og et forslag som kjenner halve historikken er
    /// halvveis nyttig.
    ///
    /// Rafornavn og torrfornavn kommer i samme liste. To lister ville betydd
    /// at et for skrevet inn pa feil felt aldri dukket opp igjen, og et
    /// forslag er uansett bare et forslag.
    /// </summary>
    Task<IReadOnlyList<string>> HentFornavn(CancellationToken ct);
}
