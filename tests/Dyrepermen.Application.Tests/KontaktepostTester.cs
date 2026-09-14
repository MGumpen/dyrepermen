using Dyrepermen.Application.Extensions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Tests;

public sealed class KontaktepostTester
{
    private static readonly DateTimeOffset Vinter = new(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Alle_typer_har_en_tekst()
    {
        // Legges det til en type uten tekst, kaster TypeTekst - og da kaster
        // ogsa kontaktsiden, som lister alle typene i nedtrekkslisten.
        foreach (var type in Enum.GetValues<Kontakttype>())
        {
            Assert.False(string.IsNullOrWhiteSpace(Kontaktepost.TypeTekst(type)));
        }
    }

    [Fact]
    public void Emnet_er_typen()
    {
        Assert.Equal("Dyrepermen: Ønske om endring", Kontaktepost.Emne(Kontakttype.Onske));
    }

    [Fact]
    public void Teksten_har_avsender_og_melding()
    {
        var tekst = Kontaktepost.Tekst(
            Kontakttype.Feil, "Knappen virker ikke.", 12, "Kari",
            "kari@example.test", Vinter);

        Assert.Contains("Type: Feil", tekst);
        Assert.Contains("Fra: Kari (bruker-ID 12)", tekst);
        Assert.Contains("E-post: kari@example.test", tekst);
        Assert.EndsWith("Knappen virker ikke.", tekst);
    }

    [Theory]
    [InlineData(1, "15.01.2026 kl. 10:30")] // Vintertid, UTC+1
    [InlineData(7, "15.07.2026 kl. 11:30")] // Sommertid, UTC+2
    public void Tidspunktet_er_norsk_lokaltid(int maned, string forventet)
    {
        var sendt = new DateTimeOffset(2026, maned, 15, 9, 30, 0, TimeSpan.Zero);

        var tekst = Kontaktepost.Tekst(
            Kontakttype.Sporsmal, "Hei", 1, "Kari", "kari@example.test", sendt);

        Assert.Contains($"Sendt: {forventet}", tekst);
    }
}
