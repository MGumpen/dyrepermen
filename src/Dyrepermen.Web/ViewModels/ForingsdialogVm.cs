using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Web.ViewModels;

/// <summary>Innholdet i foringsdialogen.</summary>
public sealed record ForingsdialogVm(
    int DyrId,
    string DyreNavn,
    Foringstype Type,
    int? MengdeGram,
    string? Fornavn,
    IReadOnlyList<string> Forslag)
{
    public bool ErGodbit => Type == Foringstype.Godbit;

    public string Tittel => ErGodbit
        ? $"Godbit til {DyreNavn}"
        : $"Gi {DyreNavn} mer mat";
}
