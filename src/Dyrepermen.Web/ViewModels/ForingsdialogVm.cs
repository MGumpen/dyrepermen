using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Web.ViewModels;

/// <summary>Innholdet i foringsdialogen.</summary>
public sealed record ForingsdialogVm(
    int DyrId,
    string DyreNavn,
    Foringstype Type,
    int? MengdeGram,
    string? Fornavn,
    IReadOnlyList<string> Forslag,

    /// <summary>
    /// Oppdelingen av porsjonen ved en overgangsplan. Dialogen er der maten
    /// faktisk veies opp, sa det er her blandingsforholdet trengs mest.
    /// Null for godbiter og for planer som ikke blander.
    /// </summary>
    Porsjonsdeling? Deling = null)
{
    public bool ErGodbit => Type == Foringstype.Godbit;

    public string Tittel => ErGodbit
        ? $"Godbit til {DyreNavn}"
        : $"Gi {DyreNavn} mer mat";
}
