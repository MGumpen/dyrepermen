using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Interfaces;

public interface IHusstandService
{
    /// <summary>
    /// Oppretter husstand med tilhorende innstillingsrad, og knytter brukeren
    /// til den. Returnerer husstandens ID.
    /// </summary>
    Task<int> OpprettHusstand(string navn, int brukerId, CancellationToken ct);

    Task<Husstandsoversikt?> HentOversikt(int brukerId, CancellationToken ct);

    /// <summary>
    /// Legger til et medlem pa e-postadresse. Finnes ikke adressen som
    /// bruker, lagres en invitasjon som loses inn ved registrering.
    /// </summary>
    Task<LeggTilResultat> LeggTilMedlem(
        string epost, Husstandsrolle rolle, int utfortAvBrukerId,
        CancellationToken ct);

    /// <summary>
    /// Endrer rollen til et medlem. Den siste eieren kan ikke degraderes -
    /// da ville innstillingene blitt last for alle.
    /// </summary>
    Task<bool> EndreRolle(int brukerId, Husstandsrolle rolle, CancellationToken ct);

    /// <summary>Sletter en ventende invitasjon. Innloste kan ikke angres.</summary>
    Task<bool> AngreInvitasjon(int invitasjonId, CancellationToken ct);

    /// <summary>
    /// Setter husstand_id til null. Alle medlemmer er likestilte, sa hvem
    /// som helst kan fjerne hvem som helst - ogsa seg selv. Data om dyrene
    /// blir vaerende i husstanden.
    /// </summary>
    Task<bool> FjernMedlem(int brukerId, CancellationToken ct);

    Task<bool> LagreInnstillinger(
        string husstandsnavn,
        bool foringsloggStandard,
        bool forplanStandard,
        bool varslerAktiv,
        bool godbitloggAktiv,
        CancellationToken ct);

    /// <summary>
    /// Loser inn en ventende invitasjon ved registrering. Kalles for
    /// brukeren har husstand, sa den kan ikke bruke query-filteret.
    /// </summary>
    Task<bool> LosInnInvitasjon(int brukerId, string epost, CancellationToken ct);
}
