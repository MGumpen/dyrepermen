using Dyrepermen.Domain.Abstractions;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Selve filen til et <see cref="Dokument"/>. Egen tabell sa en liste over
/// vedlegg ikke drar med seg megabytene - de leses bare nar filen vises.
/// </summary>
public sealed class DokumentInnhold : IHusstandsbundet
{
    public int DokumentId { get; set; }

    public Dokument Dokument { get; set; } = null!;

    public byte[] Data { get; set; } = null!;
}
