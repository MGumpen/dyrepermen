namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Kilo inn, gram i databasen. Konverteringen skjer i tjenesten, slik at
/// controlleren slipper a kjenne lagringsformatet. Se plan kapittel 5.2.
/// </summary>
public sealed record NyVekt(
    int DyrId,
    decimal Kilo,
    DateOnly Dato,
    int? RegistrertAvBrukerId);
