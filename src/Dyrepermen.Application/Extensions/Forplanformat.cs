using System.Globalization;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Extensions;

/// <summary>
/// Regelen bak formengden, uttrykt som tekst.
///
/// Appen anbefaler ikke mengde - den regner ut regelen brukeren selv har
/// lagt inn. Da ma regelen ogsa vaere synlig sammen med tallet. "410 g per
/// dag" uten grunnlag er et tall ingen vet hvor kommer fra, og en bruker som
/// ikke husker om hun valgte prosent eller fast mengde, kan ikke se det.
///
/// Teksten sto fra for i to varianter - en i InformasjonService og en
/// innbakt i utskriftsvisningen. En regel som star to steder, spriker, sa
/// den bor kun finnes her.
/// </summary>
public static class Forplanformat
{
    private static readonly CultureInfo Norsk = new("nb-NO");

    /// <summary>
    /// 50 gir "5 %", 45 gir "4,5 %". Lagringen er tidels prosent, slik at
    /// hele modellen holder seg pa heltall.
    /// </summary>
    public static string Prosenttekst(int prosentTidels)
        => string.Create(Norsk, $"{prosentTidels / 10m:0.#} %");

    /// <summary>
    /// 400 gir "400 g", 20000 gir "20 000 g". Tusenskillet er mellomrom, som
    /// ellers i appen - nb-NO bruker et hardt mellomrom, som ikke brekker
    /// tallet over to linjer.
    /// </summary>
    public static string Gramtekst(int gram)
        => string.Create(Norsk, $"{gram:#,##0} g");

    /// <summary>
    /// Regelen alene, uten resultatet: "5 % av kroppsvekten" eller
    /// "400 g per dag". Dette er svaret pa "hvor kommer tallet fra".
    /// </summary>
    public static string Regel(Formetode metode, int? prosentTidels, int? gramPerDag)
        => metode == Formetode.Prosent
            ? $"{Prosenttekst(prosentTidels ?? 0)} av kroppsvekten"
            : $"{Gramtekst(gramPerDag ?? 0)} per dag";

    /// <summary>
    /// Regnestykket bak en prosentplan, med begge leddene synlige:
    /// "8,20 kg x 5 % = 410 g". Da kan brukeren gjore regnestykket i hodet
    /// og se at det stemmer.
    /// </summary>
    public static string Utregning(int vektGram, int prosentTidels, int gramPerDag)
        => $"{Vektformat.TilKiloTekst(vektGram)} × "
         + $"{Prosenttekst(prosentTidels)} = {Gramtekst(gramPerDag)}";

    /// <summary>
    /// Kompakt sammendrag til kort og lister, der det er plass til en linje:
    /// "410 g/dag fordelt på 2 måltider (5 % av kroppsvekten)".
    ///
    /// Null nar dyret ikke har plan. Uten vektgrunnlag sier den fra i stedet
    /// for a vise et tall uten dekning - samme regel som ForplanService.
    /// </summary>
    public static string? Sammendrag(
        Formetode? metode,
        int? prosentTidels,
        int? gramPerDag,
        int? antallMaltider,
        int? sisteVektGram)
    {
        if (metode is null)
        {
            return null;
        }

        var maltider = antallMaltider ?? 2;

        if (metode == Formetode.Gram)
        {
            return $"{Gramtekst(gramPerDag ?? 0)}/dag fordelt på {maltider} måltider";
        }

        if (sisteVektGram is null)
        {
            return "Prosentplan – mangler vektregistrering";
        }

        var gram = (int)Math.Round(
            sisteVektGram.Value * (prosentTidels ?? 0) / 1000.0,
            MidpointRounding.AwayFromZero);

        return $"{Gramtekst(gram)}/dag fordelt på {maltider} måltider "
             + $"({Prosenttekst(prosentTidels ?? 0)} av kroppsvekten)";
    }
}
