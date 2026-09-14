using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Akseptansekriterium i fase 6b: «sist matet» skal vise riktig lokal tid
/// OVER sommertidsskiftet. Se plan kapittel 16.
///
/// Feilen dette fanger er ubehagelig fordi den ikke finnes nar man skriver
/// koden. Den dukker opp to ganger i aret, pa data som var riktig da den ble
/// lagret.
/// </summary>
public sealed class TidssoneTester
{
    [Fact]
    public void Vintertid_er_en_time_foran_UTC()
    {
        // 15. januar: normaltid, UTC+1.
        var utc = new DateTimeOffset(2026, 1, 15, 6, 12, 0, TimeSpan.Zero);
        Assert.Equal("07:12", Tidssone.Klokke(utc));
    }

    [Fact]
    public void Sommertid_er_to_timer_foran_UTC()
    {
        // 15. juli: sommertid, UTC+2. Samme UTC-tid som over ville gitt
        // 07:12 om man glemte konverteringen - her skal den gi 08:12.
        var utc = new DateTimeOffset(2026, 7, 15, 6, 12, 0, TimeSpan.Zero);
        Assert.Equal("08:12", Tidssone.Klokke(utc));
    }

    [Fact]
    public void Timen_for_og_etter_omstillingen_om_varen()
    {
        // Norge stiller klokka natt til siste sondag i mars 2026 = 29. mars.
        // 00:30 UTC er fortsatt vintertid -> 01:30.
        var for_ = new DateTimeOffset(2026, 3, 29, 0, 30, 0, TimeSpan.Zero);
        Assert.Equal("01:30", Tidssone.Klokke(for_));

        // 01:30 UTC er sommertid -> 03:30. Klokka hoppet over 02.
        var etter = new DateTimeOffset(2026, 3, 29, 1, 30, 0, TimeSpan.Zero);
        Assert.Equal("03:30", Tidssone.Klokke(etter));
    }

    [Fact]
    public void Timen_for_og_etter_omstillingen_om_hosten()
    {
        // Tilbake til normaltid siste sondag i oktober 2026 = 25. oktober.
        var for_ = new DateTimeOffset(2026, 10, 25, 0, 30, 0, TimeSpan.Zero);
        Assert.Equal("02:30", Tidssone.Klokke(for_));

        var etter = new DateTimeOffset(2026, 10, 25, 1, 30, 0, TimeSpan.Zero);
        Assert.Equal("02:30", Tidssone.Klokke(etter));

        // Begge gir 02:30 lokalt - den timen finnes to ganger. Det er
        // korrekt, og grunnen til at UTC er lagringsformatet.
    }

    [Fact]
    public void En_foring_registrert_om_sommeren_vises_likt_om_vinteren()
    {
        // Kjernen i kriteriet: raden endrer seg ikke, og skal vises med
        // sommertidens klokkeslett uansett nar man ser pa den.
        var registrert = new DateTimeOffset(2026, 7, 4, 5, 12, 0, TimeSpan.Zero);

        Assert.Equal("07:12", Tidssone.Klokke(registrert));
        Assert.Equal("4. juli 07:12",
            Tidssone.NaerTid(registrert, new DateTimeOffset(2026, 12, 1, 12, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void Forskyvningen_folger_arstiden()
    {
        Assert.Equal(TimeSpan.FromHours(1),
            Tidssone.Forskyvning(new DateTime(2026, 1, 15, 12, 0, 0)));
        Assert.Equal(TimeSpan.FromHours(2),
            Tidssone.Forskyvning(new DateTime(2026, 7, 15, 12, 0, 0)));
    }

    [Theory]
    [InlineData(0, "i dag 14:00")]
    [InlineData(-1, "i går 14:00")]
    public void Naere_tidspunkter_vises_relativt(int dagerSiden, string forventet)
    {
        var naa = new DateTimeOffset(2026, 7, 15, 18, 0, 0, TimeSpan.Zero);
        var tid = naa.AddDays(dagerSiden).AddHours(-6);

        Assert.Equal(forventet, Tidssone.NaerTid(tid, naa));
    }
}
