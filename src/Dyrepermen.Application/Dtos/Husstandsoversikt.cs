namespace Dyrepermen.Application.Dtos;

public sealed record Husstandsoversikt(
    int HusstandId,
    string Navn,
    IReadOnlyList<Husstandsmedlem> Medlemmer,
    IReadOnlyList<VentendeInvitasjon> Ventende,
    bool ForingsloggStandard,
    bool ForplanStandard,
    bool VarslerAktiv,
    bool GodbitloggAktiv,
    int AntallDyr);
