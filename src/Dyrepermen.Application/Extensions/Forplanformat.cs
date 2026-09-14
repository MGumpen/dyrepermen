using System.Globalization;
using Dyrepermen.Application.Dtos;
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
    /// Regelen i en kort setning, uten resultatet. Dette er svaret pa "hvor
    /// kommer tallet fra", og det skal kunne leses av noen som ikke har lest
    /// noe annet i appen.
    /// </summary>
    public static string Regel(
        Formetode metode,
        int? prosentTidels,
        int? gramPerDag,
        int? vektdelAndelProsent = null)
        => metode switch
        {
            Formetode.Prosent
                => $"{Prosenttekst(prosentTidels ?? 0)} av kroppsvekten",

            Formetode.Gram
                => $"{Gramtekst(gramPerDag ?? 0)} per dag",

            _ => Tabellregel(prosentTidels ?? 0, vektdelAndelProsent ?? 0)
        };

    /// <summary>
    /// Tabellmetoden i tre varianter: ren tabell, rent vektmalt for, og
    /// blandingen mellom dem. Andelen 0 og 100 er ikke spesialtilfeller a
    /// beklage - de er de to endepunktene i en forovergang.
    ///
    /// Teksten sier hvordan mengden MALES, ikke hva foret er. En overgang
    /// kan ga begge veier, og mellom hva som helst.
    /// </summary>
    public static string Tabellregel(int prosentTidels, int vektdelAndelProsent)
        => vektdelAndelProsent switch
        {
            0 => "Tabellen på fôrposen, etter alder",

            100 => $"{Prosenttekst(prosentTidels)} av kroppsvekten",

            _ => $"{vektdelAndelProsent} % etter vekt og "
               + $"{100 - vektdelAndelProsent} % etter alder"
        };

    /// <summary>
    /// Ett ledd i blandingen med hele regnestykket synlig:
    /// "410 g x 70 % = 287 g". Brukeren skal kunne regne etter selv.
    /// </summary>
    public static string Delutregning(int fullGram, int andelProsent, int gram)
        => $"{Gramtekst(fullGram)} × {andelProsent} % = {Gramtekst(gram)}";

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
    /// "410 g/dag fordelt pa 2 maltider (5 % av kroppsvekten)".
    ///
    /// Regner ingenting selv. Tidligere gjorde den det, med sin egen kopi av
    /// prosentregelen - og da kunne dyrekortet vise et annet tall enn
    /// forplansiden uten at noe sa fra. Na kommer tallet fra
    /// <see cref="Forberegning"/>, og denne klassen setter bare ord pa det.
    ///
    /// Null nar dyret ikke har plan. Uten grunnlag sier den fra i stedet for
    /// a vise et tall uten dekning.
    /// </summary>
    public static string? Sammendrag(Forplanregel? regel, ForplanResultat resultat)
    {
        if (regel is null || !resultat.HarPlan)
        {
            return null;
        }

        if (resultat.ManglerVekt)
        {
            return "Mangler vektregistrering";
        }

        if (resultat.ManglerFodselsdato)
        {
            return "Mangler fødselsdato";
        }

        var start = $"{Gramtekst(resultat.GramPerDag)}/dag fordelt på "
                  + $"{resultat.AntallMaltider} måltider";

        return (regel.Metode, resultat.Fordeling) switch
        {
            (Formetode.Gram, _) => start,

            (Formetode.Prosent, _)
                => $"{start} ({Prosenttekst(regel.ProsentTidels ?? 0)} av kroppsvekten)",

            // Blander planen, ma begge tallene sta - summen alene sier ikke
            // hvor mye av hver som skal veies opp.
            (_, { Blander: true } f)
                => $"{start} ({Gramtekst(f.VektdelGram)} etter vekt + "
                 + $"{Gramtekst(f.AldersdelGram)} etter alder)",

            (_, { VektdelAndelProsent: 100 })
                => $"{start} ({Prosenttekst(regel.ProsentTidels ?? 0)} av kroppsvekten)",

            _ => $"{start} (etter alder)"
        };
    }
}
