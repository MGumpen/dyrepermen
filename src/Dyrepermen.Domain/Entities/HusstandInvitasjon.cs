using Dyrepermen.Domain.Abstractions;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Forhandsgodkjent e-postadresse. Registrerer noen seg med noyaktig denne
/// adressen, knyttes de automatisk til husstanden. Ingen kode a taste inn.
/// Se plan kapittel 12.3.
/// </summary>
public sealed class HusstandInvitasjon : IHusstandsbundet
{
    public int Id { get; set; }

    public int HusstandId { get; set; }

    public Husstand Husstand { get; set; } = null!;

    /// <summary>Normalisert til sma bokstaver for lagring.</summary>
    public string Epost { get; set; } = null!;

    /// <summary>Satt nar adressen registrerte seg.</summary>
    public int? InnlostAvBrukerId { get; set; }

    public Bruker? InnlostAv { get; set; }

    public DateTimeOffset? InnlostTid { get; set; }

    public int? OpprettetAvBrukerId { get; set; }

    public Bruker? OpprettetAv { get; set; }

    public DateOnly OpprettetDato { get; set; }
}
