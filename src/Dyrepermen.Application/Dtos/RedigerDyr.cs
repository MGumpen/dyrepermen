using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Inndata ved redigering. Her er funksjonsbryterne med - de betjenes pa
/// dyrets egen redigeringsside, ikke pa en separat innstillingsside.
/// </summary>
public sealed record RedigerDyr(
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
