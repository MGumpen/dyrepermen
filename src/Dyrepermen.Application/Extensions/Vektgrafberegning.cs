using System.Globalization;
using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Application.Extensions;

/// <summary>
/// Regner om vektmalinger til SVG-koordinater.
///
/// X-aksen folger FAKTISK TID, ikke malingenes rekkefolge. Tre malinger i
/// januar og en i desember skal ikke ligge jevnt fordelt - da ville grafen
/// vist en jevn vekst som ikke fant sted.
/// </summary>
public static class Vektgrafberegning
{
    public const double Bredde = 640;
    public const double Hoyde = 200;

    private const double Venstre = 52;   // plass til y-etiketter
    private const double Hoyre = 16;
    private const double Topp = 14;
    private const double Bunn = 28;      // plass til datoer

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>
    /// Null ved faerre enn to malinger. En enkelt maling er et tall, ikke en
    /// graf - da sier historikktabellen det som er a si.
    /// </summary>
    public static Vektgrafdata? Beregn(IReadOnlyList<(DateOnly Dato, int Gram)> malinger)
    {
        if (malinger.Count < 2)
        {
            return null;
        }

        var sortert = malinger.OrderBy(m => m.Dato).ToList();

        var forste = sortert[0].Dato;
        var siste = sortert[^1].Dato;
        var dagerTotalt = Math.Max((siste.ToDateTime(TimeOnly.MinValue)
                                  - forste.ToDateTime(TimeOnly.MinValue)).TotalDays, 1);

        var minGram = sortert.Min(m => m.Gram);
        var maksGram = sortert.Max(m => m.Gram);

        // Litt luft over og under, sa punktene ikke klistrer seg til kanten.
        // Er alle malingene like, lages et kunstig spenn - ellers blir
        // divisjonen under null.
        var spenn = Math.Max(maksGram - minGram, 1);
        var luft = Math.Max(spenn * 0.15, 100);

        var (bunnGram, toppGram, steg) = Akse(minGram - luft, maksGram + luft);

        double YFor(int gram)
            => Topp + (Hoyde - Topp - Bunn)
                    * (1 - (gram - bunnGram) / (double)(toppGram - bunnGram));

        double XFor(DateOnly dato)
            => Venstre + (Bredde - Venstre - Hoyre)
                       * ((dato.ToDateTime(TimeOnly.MinValue)
                         - forste.ToDateTime(TimeOnly.MinValue)).TotalDays / dagerTotalt);

        var punkter = sortert
            .Select(m => new Grafpunkt(m.Dato, m.Gram, XFor(m.Dato), YFor(m.Gram)))
            .ToList();

        var merker = new List<Aksemerke>();
        for (var g = bunnGram; g <= toppGram; g += steg)
        {
            merker.Add(new Aksemerke(YFor(g), g));
        }

        var linje = string.Join(" ", punkter.Select((p, i) =>
            string.Create(Inv, $"{(i == 0 ? "M" : "L")}{p.X:0.##},{p.Y:0.##}")));

        // Flaten under linja, lukket mot bunnlinja.
        var flate = string.Create(Inv,
            $"{linje} L{punkter[^1].X:0.##},{Hoyde - Bunn} L{punkter[0].X:0.##},{Hoyde - Bunn} Z");

        return new Vektgrafdata(punkter, merker, linje, flate, Bredde, Hoyde);
    }

    /// <summary>
    /// Runde aksemerker. Uten dette blir etikettene 3,47 kg og 4,12 kg -
    /// tall ingen leser av en akse.
    /// </summary>
    private static (int Bunn, int Topp, int Steg) Akse(double lav, double hoy)
    {
        var spenn = Math.Max(hoy - lav, 1);
        // Fire intervaller gir rundt fem streker. Tre ga sa grove steg at
        // dataene ble klemt sammen midt i flaten.
        var raatt = spenn / 4.0;

        var storrelse = Math.Pow(10, Math.Floor(Math.Log10(raatt)));
        var normalisert = raatt / storrelse;

        var pentTall = normalisert switch
        {
            <= 1 => 1,
            <= 2 => 2,
            <= 2.5 => 2.5,
            <= 5 => 5,
            _ => 10
        };

        var steg = Math.Max((int)Math.Round(pentTall * storrelse), 1);
        var bunn = (int)(Math.Floor(Math.Max(lav, 0) / steg) * steg);
        var topp = (int)(Math.Ceiling(hoy / steg) * steg);

        return (bunn, Math.Max(topp, bunn + steg), steg);
    }
}
