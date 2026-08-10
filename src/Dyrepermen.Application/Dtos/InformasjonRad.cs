namespace Dyrepermen.Application.Dtos;

public sealed record InformasjonRad(
    int Id,
    string Tittel,
    string Tekst,
    int? DyrId,
    string? DyreNavn,
    DateOnly OpprettetDato);
