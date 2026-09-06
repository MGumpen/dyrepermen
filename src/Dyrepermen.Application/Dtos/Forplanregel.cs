using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Regelen brukeren har lagt inn, uten noe som er regnet ut av den.
///
/// Dette er inndataen til <c>Forberegning.Beregn</c>. Alle stedene som viser
/// en formengde - forplansiden, dashbordet, dyrekortet, utskriften - fyller
/// den samme typen og far det samme svaret. For fantes regnestykket i fire
/// kopier, og en regel som star fire steder, spriker.
/// </summary>
public sealed record Forplanregel(
    Formetode Metode,
    int? ProsentTidels,
    int? GramPerDag,
    int AntallMaltider,
    int? VektdelAndelProsent,
    IReadOnlyList<Alderstrinn> Tabelltrinn)
{
    /// <summary>For de to enkle metodene, der torrfortabellen ikke finnes.</summary>
    public Forplanregel(
        Formetode metode, int? prosentTidels, int? gramPerDag, int antallMaltider)
        : this(metode, prosentTidels, gramPerDag, antallMaltider, null, [])
    {
    }
}
