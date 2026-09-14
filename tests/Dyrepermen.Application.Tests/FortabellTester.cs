using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Torrformengden slas opp i brukerens egen tabell. Appen anbefaler
/// ingenting - den trapper jevnt mellom radene og holder tallet stille en
/// uke om gangen.
/// </summary>
public sealed class FortabellTester
{
    // Tabellen pa en typisk valpepose: to trinn med 20 gram imellom.
    private static readonly IReadOnlyList<Alderstrinn> Posen =
    [
        new Alderstrinn(3, 160),
        new Alderstrinn(4, 180)
    ];

    [Fact]
    public void Tom_tabell_gir_null_gram()
        => Assert.Equal(0, Fortabell.GramVedUker([], 17));

    /// <summary>
    /// Under forste rad ekstrapoleres det ikke bakover. En valp pa tolv uker
    /// skal ikke fa et tall regnet ut fra en kurve som starter pa tretten.
    /// </summary>
    [Fact]
    public void Under_forste_rad_gjelder_forste_rad()
        => Assert.Equal(160, Fortabell.GramVedUker(Posen, 12));

    [Fact]
    public void Over_siste_rad_gjelder_siste_rad()
        => Assert.Equal(180, Fortabell.GramVedUker(Posen, 60));

    /// <summary>
    /// Mellom radene trappes det jevnt. 15 uker er 3,45 maneder, altsa 45 %
    /// av veien fra 160 til 180.
    /// </summary>
    [Theory]
    [InlineData(13, 160)]
    [InlineData(14, 164)]
    [InlineData(15, 169)]
    [InlineData(17, 178)]
    [InlineData(18, 180)]
    public void Mellom_to_rader_trappes_mengden_jevnt(int uker, int forventet)
        => Assert.Equal(forventet, Fortabell.GramVedUker(Posen, uker));

    /// <summary>
    /// Den viktigste: mengden endrer seg ETT hopp i uka, ikke litt hver dag.
    /// En tabell som aldri star stille er umulig a mate etter.
    /// </summary>
    [Fact]
    public void Alle_dagene_i_en_uke_gir_samme_mengde()
    {
        var fodt = new DateOnly(2026, 5, 1);

        var iUka = Enumerable.Range(0, 7)
            .Select(d => Fortabell.GramVedUker(
                Posen, Alderformat.UkerSiden(fodt, fodt.AddDays(105 + d))))
            .Distinct()
            .ToList();

        Assert.Single(iUka);

        // ...men neste uke skal den ha flyttet seg.
        Assert.NotEqual(
            iUka[0],
            Fortabell.GramVedUker(
                Posen, Alderformat.UkerSiden(fodt, fodt.AddDays(112))));
    }

    [Fact]
    public void En_enkelt_rad_gjelder_hele_livet()
        => Assert.Equal(
            200, Fortabell.GramVedUker([new Alderstrinn(6, 200)], 3));

    [Fact]
    public void Radene_trenger_ikke_komme_i_rekkefolge()
        => Assert.Equal(
            164,
            Fortabell.GramVedUker(
                [new Alderstrinn(4, 180), new Alderstrinn(3, 160)], 14));

    /// <summary>
    /// Oppslaget sier fra nar alderen faller utenfor tabellen. Da holdes
    /// mengden pa naermeste rad, og grensesnittet kan be om en rad til i
    /// stedet for a la et flatt tall se ut som en beregning.
    /// </summary>
    [Theory]
    [InlineData(12, true)]    // yngre enn forste rad
    [InlineData(14, false)]   // midt i tabellen
    [InlineData(60, true)]    // eldre enn siste rad
    public void Oppslaget_sier_fra_nar_alderen_er_utenfor(int uker, bool forventet)
        => Assert.Equal(forventet, Fortabell.Slaopp(Posen, uker).UtenforTabellen);

    /// <summary>
    /// Noyaktig pa en rad er innenfor, ikke utenfor. Ellers ville en tabell
    /// som slutter pa 12 mnd klaget pa en hund som er akkurat 12 mnd.
    /// </summary>
    [Fact]
    public void Noyaktig_pa_siste_rad_er_innenfor()
    {
        // 4 mnd er 17,4 uker. 18 uker er like over, 17 like under.
        Assert.False(Fortabell.Slaopp(Posen, 17).UtenforTabellen);
        Assert.True(Fortabell.Slaopp(Posen, 18).UtenforTabellen);
    }

    [Fact]
    public void Tom_tabell_er_ikke_utenfor_noe_som_helst()
        => Assert.False(Fortabell.Slaopp([], 17).UtenforTabellen);
}
