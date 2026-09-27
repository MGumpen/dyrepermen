namespace Dyrepermen.Domain.Enums;

/// <summary>Lagres som char(1): V, J, K, A eller P.</summary>
public enum DokumentKategori
{
    Vaksinebok,
    Journal,
    Kvittering,
    Annet,

    /// <summary>
    /// Bildet av dyret pa dashbordet, dyrets side og savnet-plakaten. Hoyst
    /// ett per dyr - sikret av en unik indeks i databasen.
    /// </summary>
    Profilbilde
}
