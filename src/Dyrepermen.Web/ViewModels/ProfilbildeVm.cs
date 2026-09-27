namespace Dyrepermen.Web.ViewModels;

/// <summary>
/// Dyrets bilde, eller forbokstaven nar det ikke har noe. Delt av
/// dashbordet, dyrets side og redigeringssiden, sa de ser like ut.
/// </summary>
/// <param name="Storrelse">CSS-klassen: "liten", "stor" eller "plakat".</param>
public sealed record ProfilbildeVm(int? DokumentId, string Navn, string Storrelse);
