using Dyrepermen.Domain.Abstractions;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Standardverdier som kopieres til nye dyr ved opprettelse.
///
/// Dette er en malverdi, ikke et overstyringsniva. Den overstyrer aldri en
/// bryter som allerede star pa et dyr. Med to virkelige niva oppstar
/// sporsmalet "husstand av, dyr pa - hva gjelder?", og hvert svar er feil for
/// noen. Med standardverdi finnes ikke sporsmalet. Se plan kapittel 8.2.
/// </summary>
public sealed class HusstandInnstilling : IHusstandsbundet
{
    /// <summary>Bade primaernokkel og fremmednokkel.</summary>
    public int HusstandId { get; set; }

    public Husstand Husstand { get; set; } = null!;

    public bool ForingsloggStandard { get; set; }

    public bool ForplanStandard { get; set; } = true;

    /// <summary>Gjelder e-postutsending, og er pa husstandsniva.</summary>
    public bool VarslerAktiv { get; set; } = true;
}
