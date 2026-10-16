using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

/// <summary>
/// En kur med "Gitt"-knapp, pa oversikten eller pa dyrets side.
/// <paramref name="FraDyr"/> styrer hva raden heter - dyrets navn pa
/// oversikten, behandlingen pa dyrets side - og hvor brukeren havner uten
/// htmx. Knappen sender flagget med, sa raden som kommer tilbake, ser ut
/// som den den erstatter.
/// </summary>
public sealed record KurradVm(Kurrad Rad, bool FraDyr = false);
