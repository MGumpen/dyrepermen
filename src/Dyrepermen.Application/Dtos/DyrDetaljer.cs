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
    string? Farge,
    string? Kjennetegn,

    /// <summary>
    /// Dokument-id-en til profilbildet, eller null. Bildet selv hentes via
    /// /dokument/{id}, som verifiserer husstanden.
    /// </summary>
    int? ProfilbildeId,

    bool ForingsloggAktiv,
    bool ForplanAktiv);
