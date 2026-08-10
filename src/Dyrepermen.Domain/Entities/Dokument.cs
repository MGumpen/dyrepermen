using Dyrepermen.Domain.Abstractions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Filer serveres via en controller-handling som verifiserer
/// husstandstilhorighet, aldri direkte fra wwwroot. Se plan kapittel 15.
/// </summary>
public sealed class Dokument : IHusstandsbundet
{
    public int Id { get; set; }

    public int DyrId { get; set; }

    public Dyr Dyr { get; set; } = null!;

    /// <summary>Lagret navn: generert GUID med filendelse.</summary>
    public string Filnavn { get; set; } = null!;

    /// <summary>Brukerens eget filnavn, vises i grensesnittet.</summary>
    public string Originalnavn { get; set; } = null!;

    public DokumentKategori Kategori { get; set; }

    public DateOnly OpplastetDato { get; set; }
}
