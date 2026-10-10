using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Application.Dtos;

/// <summary>
/// En kur pa oversikten, slik raden med "Gitt"-knappen tegnes. Dashbordet
/// bygger den fra sporring 1, og knappen far den tilbake fra
/// IBehandlingService.HentKurrad nar bare raden skal tegnes pa nytt.
/// </summary>
public sealed record Kurrad(
    int DyrId,
    string DyreNavn,
    int BehandlingId,

    /// <summary>"Ormekur – Milbemax".</summary>
    string Beskrivelse,

    /// <summary>Dagnummeret i kuren i dag, 1 pa forste dag.</summary>
    int Dag,
    int AntallDager,
    int GangerPerDag,

    /// <summary>Doser registrert siden norsk midnatt.</summary>
    int DoserIdag,

    /// <summary>
    /// Kuren gar i dag. False nar raden tegnes pa nytt fra en fane som har
    /// statt apen forbi siste dag - da er det ingenting a gi.
    /// </summary>
    bool Aktiv)
{
    public bool FullfortIdag => DoserIdag >= GangerPerDag;

    public bool KanGis => Aktiv && !FullfortIdag;

    public string Fremdrift
        => Behandlingskur.Fremdrift(Dag, AntallDager, DoserIdag, GangerPerDag);
}
