using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

public sealed record DyrDetaljer(
    int Id,
    string Navn,
    Art Art,
    Kjonn Kjonn,
    string? Rase,
    DateOnly? Fodselsdato,
    string? ChipNr,
    string? RegNrNkk,
    bool Kastrert,
    bool ForingsloggAktiv,
    bool ForplanAktiv);
