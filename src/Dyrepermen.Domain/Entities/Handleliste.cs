using Dyrepermen.Domain.Abstractions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Henger pa husstanden, ikke pa dyret - dette er husstandsoppgaver.
/// <see cref="DyrId"/> er en valgfri kobling; uten den vises punktet
/// som "Felles". Se plan kapittel 5.2.
/// </summary>
public sealed class Handleliste : IHusstandsbundet
{
    public int Id { get; set; }

    public int HusstandId { get; set; }

    public Husstand Husstand { get; set; } = null!;

    public int? DyrId { get; set; }

    public Dyr? Dyr { get; set; }

    public string Tekst { get; set; } = null!;

    public int Antall { get; set; } = 1;

    public HandlelisteStatus Status { get; set; } = HandlelisteStatus.Aktiv;

    public int? OpprettetAvBrukerId { get; set; }

    public Bruker? OpprettetAv { get; set; }

    public DateOnly OpprettetDato { get; set; }
}
