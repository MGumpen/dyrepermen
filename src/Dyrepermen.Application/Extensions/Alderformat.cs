namespace Dyrepermen.Application.Extensions;

public static class Alderformat
{
    /// <summary>
    /// Hele ar. Under ett ar oppgis i maneder - "3 mnd" er nyttig for en
    /// valp, "0 ar" er det ikke.
    /// </summary>
    public static string Tekst(DateOnly fodselsdato, DateOnly idag)
    {
        var ar = idag.Year - fodselsdato.Year;
        if (fodselsdato > idag.AddYears(-ar))
        {
            ar--;
        }

        if (ar >= 1)
        {
            return ar == 1 ? "1 år" : $"{ar} år";
        }

        var mnd = (idag.Year - fodselsdato.Year) * 12 + idag.Month - fodselsdato.Month;
        if (fodselsdato.Day > idag.Day)
        {
            mnd--;
        }

        mnd = Math.Max(mnd, 0);
        return mnd == 1 ? "1 mnd" : $"{mnd} mnd";
    }
}
