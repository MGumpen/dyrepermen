namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Tenant-roten. Alle husstandsbundne sporringer filtreres pa <see cref="Id"/>.
///
/// Har bevisst ingen ON DELETE CASCADE fra seg selv - sletting av en husstand
/// skal aldri skje utilsiktet. Se plan kapittel 5.2.
/// </summary>
public sealed class Husstand
{
    public int Id { get; set; }

    public string Navn { get; set; } = null!;

    public DateOnly OpprettetDato { get; set; }

    public ICollection<Bruker> Medlemmer { get; set; } = new List<Bruker>();

    public ICollection<Dyr> Dyr { get; set; } = new List<Dyr>();

    public ICollection<Handleliste> Handleliste { get; set; } = new List<Handleliste>();

    public ICollection<HusstandInvitasjon> Invitasjoner { get; set; }
        = new List<HusstandInvitasjon>();

    public HusstandInnstilling? Innstilling { get; set; }
}
