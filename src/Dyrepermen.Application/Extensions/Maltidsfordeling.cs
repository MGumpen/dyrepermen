namespace Dyrepermen.Application.Extensions;

public static class Maltidsfordeling
{
    /// <summary>
    /// Fordeler dagsmengden pa maltider med heltallsdivisjon. Resten legges
    /// pa de forste maltidene, slik at summen alltid stemmer eksakt med
    /// dagsmengden. Se plan kapittel 8.1.
    ///
    /// 401 gram pa 3 maltider gir [134, 134, 133] - ikke [133, 133, 133],
    /// som ville "mistet" ett gram hver dag.
    /// </summary>
    public static int[] Fordel(int gramPerDag, int antall)
    {
        if (antall <= 0)
        {
            return [];
        }

        var basis = gramPerDag / antall;
        var rest = gramPerDag % antall;

        return Enumerable.Range(0, antall)
            .Select(i => basis + (i < rest ? 1 : 0))
            .ToArray();
    }
}
