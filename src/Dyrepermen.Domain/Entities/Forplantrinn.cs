using Dyrepermen.Domain.Abstractions;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Ett trinn i torrfortabellen til en blandingsplan: "ved fire maneder, 180
/// gram per dag". Tallene star pa forposen - appen anbefaler dem ikke, den
/// slar opp i tabellen brukeren selv har lagt inn.
///
/// Trinnene horer til planen, ikke til dyret. Erstattes planen, folger en ny
/// tabell med, og den gamle blir staende sammen med den gamle planen som
/// revisjonsspor.
/// </summary>
public sealed class Forplantrinn : IHusstandsbundet
{
    public int Id { get; set; }

    public int ForplanId { get; set; }

    public Forplan Forplan { get; set; } = null!;

    /// <summary>Alder i hele maneder, slik forposene er merket.</summary>
    public int AlderMnd { get; set; }

    public int GramPerDag { get; set; }
}
