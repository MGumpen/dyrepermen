namespace Dyrepermen.Application.Dtos;

/// <summary>
/// En rad i "Forfaller snart".
///
/// <see cref="KildeId"/> er id-en i tabellen <see cref="Kilde"/> peker pa -
/// behandlingen, medisinen, forsikringen eller besoket. Den og
/// <see cref="DyrId"/> er det som skal til for a folge opp raden rett fra
/// dashbordet, uten a lete den fram pa dyrets side.
/// </summary>
public sealed record Paminnelse(
    int DyrId,
    string DyreNavn,
    Kilde Kilde,
    int KildeId,
    string Tekst,
    DateOnly Dato,

    /// <summary>
    /// Forfalt tidligere i dag. Kun medisindoser har klokkeslett - for resten
    /// er datoen hele sannheten.
    /// </summary>
    bool Overtid = false,

    /// <summary>
    /// Raden kan folges opp herfra. False for en behandling som ble gitt i
    /// dag, der "gitt" ville gitt en kopi. Se Behandlingsintervall.KanGisIgjen.
    /// </summary>
    bool KanFolgesOpp = true)
{
    public bool ErForfalt(DateOnly idag) => Dato < idag || Overtid;
}
