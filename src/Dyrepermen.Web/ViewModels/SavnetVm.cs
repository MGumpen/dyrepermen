using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

/// <summary>
/// Savnet-plakaten. Kontaktfeltene fylles inn fra den innloggede brukeren,
/// men kan endres pa siden for utskrift - og det som endres der, lagres ikke.
/// </summary>
public sealed record SavnetVm(
    DyrDetaljer Dyr,
    string Kontaktnavn,
    string? Telefon);
