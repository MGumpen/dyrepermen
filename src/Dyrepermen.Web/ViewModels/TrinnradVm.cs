using System.ComponentModel.DataAnnotations;

namespace Dyrepermen.Web.ViewModels;

/// <summary>
/// En rad i torrfortabellen. Begge feltene er nullbare fordi skjemaet viser
/// et fast antall tomme rader - brukeren fyller ut de hun har tall for, og
/// resten ignoreres.
/// </summary>
public sealed class TrinnradVm
{
    [Range(0, 240, ErrorMessage = "Alderen må være mellom 0 og 240 måneder.")]
    [Display(Name = "Alder i måneder")]
    public int? AlderMnd { get; set; }

    [Range(1, 20000, ErrorMessage = "Mengden må være mellom 1 og 20 000 gram.")]
    [Display(Name = "Gram per dag")]
    public int? GramPerDag { get; set; }

    public bool ErUtfylt => AlderMnd is not null && GramPerDag is not null;

    public bool ErTom => AlderMnd is null && GramPerDag is null;
}
