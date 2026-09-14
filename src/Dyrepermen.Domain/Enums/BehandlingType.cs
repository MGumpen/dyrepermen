namespace Dyrepermen.Domain.Enums;

/// <summary>
/// Lagres som char(1): V, O, F, K, T eller A.
///
/// Annet star sist og er en apen kategori: alt som ikke passer i de fem
/// faste, som klipp, bad eller en kontroll uten navn. Da ma preparatfeltet
/// fylles ut, ellers star det bare "Annet" i historikken.
/// </summary>
public enum BehandlingType
{
    Vaksine,
    Ormekur,
    Flatt,
    Kloklipp,
    Tannrens,
    Annet
}
