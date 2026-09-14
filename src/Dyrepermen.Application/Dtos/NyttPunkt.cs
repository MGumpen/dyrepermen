namespace Dyrepermen.Application.Dtos;

public sealed record NyttPunkt(
    string Tekst,
    int Antall,
    int? DyrId,
    int? OpprettetAvBrukerId);
