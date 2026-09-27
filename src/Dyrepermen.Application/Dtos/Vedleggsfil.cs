namespace Dyrepermen.Application.Dtos;

/// <summary>Selve filen, klar til a sendes til nettleseren.</summary>
public sealed record Vedleggsfil(string Navn, string Innholdstype, byte[] Data);
