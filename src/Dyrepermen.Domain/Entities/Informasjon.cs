using Dyrepermen.Domain.Abstractions;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Fritekstnotat knyttet til husstanden, eventuelt til ett bestemt dyr.
///
/// "Spiser ikke for kl. 07", "Er redd for torden", "Vet: Dyreklinikken
/// Arendal, 37 00 00 00". Dette er kunnskapen som ellers bor i hodet til
/// den ene av de to voksne, og som den andre trenger nar hun star der alene.
///
/// <see cref="DyrId"/> er nullbar. Uten dyr er notatet husstandens felles,
/// og vises som "Felles" - samme monster som handlelisten.
///
/// Ikke i plan kapittel 5. Se docs/beslutninger/0008-informasjonsnotater.md
/// </summary>
public sealed class Informasjon : IHusstandsbundet
{
    public int Id { get; set; }

    public int HusstandId { get; set; }

    public Husstand Husstand { get; set; } = null!;

    public int? DyrId { get; set; }

    public Dyr? Dyr { get; set; }

    public string Tittel { get; set; } = null!;

    public string Tekst { get; set; } = null!;

    public int? OpprettetAvBrukerId { get; set; }

    public Bruker? OpprettetAv { get; set; }

    public DateOnly OpprettetDato { get; set; }
}
