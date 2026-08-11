using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// "Hvor mange maltider er gitt i dag" ma forankres i norsk midnatt.
/// Regnestykket er lite, men feilen det hindrer er stygg: kveldsmaten ville
/// blitt talt som morgendagens.
/// </summary>
public sealed class DagStartTester
{
    private static DateTimeOffset Utc(
        int ar, int mnd, int dag, int time, int min = 0)
        => new(ar, mnd, dag, time, min, 0, TimeSpan.Zero);

    [Fact]
    public void Sommertid_starter_dagen_klokka_22_UTC_dagen_for()
    {
        // 11. august er Norge pa +02:00. Lokal midnatt er da 22:00 UTC
        // kvelden for.
        Assert.Equal(
            Utc(2026, 8, 10, 22),
            Tidssone.DagStart(Utc(2026, 8, 11, 10)));
    }

    [Fact]
    public void Vintertid_starter_dagen_klokka_23_UTC_dagen_for()
    {
        // 15. januar er Norge pa +01:00.
        Assert.Equal(
            Utc(2026, 1, 14, 23),
            Tidssone.DagStart(Utc(2026, 1, 15, 10)));
    }

    [Fact]
    public void Rett_over_midnatt_regnes_som_den_nye_dagen()
    {
        // Dette er hele poenget. Klokka 00:30 norsk tid 11. august er
        // UTC-klokka 22:30 den 10. Hadde vi talt fra UTC-midnatt, ville
        // grensen ligget pa 10. august 00:00 - og hele gardagens foringer
        // blitt talt med som "i dag".
        var likEtterMidnatt = Utc(2026, 8, 10, 22, 30);

        var start = Tidssone.DagStart(likEtterMidnatt);

        Assert.Equal(Utc(2026, 8, 10, 22), start);

        // Kveldsmaten i gar, klokka 21 norsk tid, faller utenfor.
        Assert.True(Utc(2026, 8, 10, 19) < start);
    }

    [Fact]
    public void Dagen_omstillingen_skjer_bruker_forskyvningen_ved_midnatt()
    {
        // 25. oktober 2026 stilles klokka tilbake klokka 03:00 lokal tid.
        // Ved midnatt gjelder fortsatt +02:00, sa dagen starter 23:00 UTC
        // kvelden for. Henter man forskyvningen pa ETTERMIDDAGEN i stedet,
        // far man +01:00 og bommer med en time.
        Assert.Equal(
            Utc(2026, 10, 24, 22),
            Tidssone.DagStart(Utc(2026, 10, 25, 14)));
    }
}
