namespace Dyrepermen.Web.ViewModels;

/// <summary>
/// Sidehodet pa en underside av et dyr - vekt, forplan, behandling, medisin,
/// foring og redigering.
///
/// Alle seks sidene hadde det samme hodet skrevet ut hver for seg, og bare
/// overskriften skilte dem. Da tilbakelenken skulle inn, matte den ellers
/// legges inn seks ganger og holdes lik seks steder.
/// </summary>
/// <param name="Tittel">Hva siden handler om: "Vekt", "Fôrplan", "Medisin".</param>
/// <param name="DyrId">Dyret man kom fra, og gar tilbake til.</param>
/// <param name="DyrNavn">Navnet, som star i tilbakelenken.</param>
public sealed record DyrhodeVm(
    string Tittel,
    int DyrId,
    string DyrNavn);
