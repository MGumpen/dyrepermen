using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Alt vi vet om ett dyr, samlet. Brukes pa informasjonssiden - en side du
/// kan vise fram til dyrepasseren.
/// </summary>
public sealed record DyreOversikt(
    int Id,
    string Navn,
    Art Art,
    Kjonn Kjonn,
    string? Rase,
    DateOnly? Fodselsdato,
    string? ChipNr,
    string? RegNrNkk,
    bool Kastrert,
    int? SisteVektGram,
    DateOnly? SisteVektDato,
    IReadOnlyList<string> AktiveMedisiner,
    string? ForplanTekst,
    IReadOnlyList<InformasjonRad> Notater);
