using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

/// <summary>
/// Dyrelisten pa dashbordet. Godbitbryteren er husstandsniva, ikke per dyr,
/// sa den folger listen framfor a gjentas pa hvert kort.
/// </summary>
public sealed record DyrelisteVm(
    IReadOnlyList<DyrKort> Dyr,
    bool GodbitloggAktiv);
