namespace Dyrepermen.Application.Interfaces;

/// <summary>
/// Gjeldende husstand for foresporselen. Leses av hvert globale query-filter
/// i DyrepermenDbContext.
///
/// Verdien 0 betyr "ikke satt" og gir tomt resultatsett overalt - fail closed.
/// </summary>
public interface IHusstandContext
{
    int HusstandId { get; }
}
