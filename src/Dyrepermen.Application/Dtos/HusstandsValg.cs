using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

/// <summary>En husstand brukeren er med i, til velgeren i sidemenyen.</summary>
public sealed record HusstandsValg(
    int Id,
    string Navn,
    Husstandsrolle Rolle,
    bool ErAktiv);
