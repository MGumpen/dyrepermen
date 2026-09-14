namespace Dyrepermen.Application.Dtos;

public sealed record Paminnelse(
    string DyreNavn,
    Kilde Kilde,
    string Tekst,
    DateOnly Dato)
{
    public bool ErForfalt(DateOnly idag) => Dato < idag;
}
