namespace Dyrepermen.Application.Dtos;

public sealed record VentendeInvitasjon(
    int Id,
    string Epost,
    DateOnly OpprettetDato);
