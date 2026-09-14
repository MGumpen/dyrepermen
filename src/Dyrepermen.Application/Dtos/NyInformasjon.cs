namespace Dyrepermen.Application.Dtos;

public sealed record NyInformasjon(
    int? Id,
    string Tittel,
    string Tekst,
    int? DyrId,
    int? OpprettetAvBrukerId);
