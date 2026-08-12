using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Lenkene i veterinaerkortet. Ren logikk, men den avgjor om et trykk ringer
/// eller apner riktig nettside - og begge feiler stille hvis de er gale.
/// </summary>
public sealed class VeterinarlenkeTester
{
    private static Veterinarrad Sted(string? telefon = null, string? nettside = null)
        => new(1, "Klinikken", Veterinartype.Fast, telefon, null,
               nettside, null, null, null, 0);

    [Theory]
    [InlineData("koba-vets.no", "https://koba-vets.no")]
    [InlineData("www.koba-vets.no", "https://www.koba-vets.no")]
    public void Nettside_uten_protokoll_far_https(string skrevet, string forventet)
    {
        // Uten protokoll tolker nettleseren href-en som en RELATIV sti, og
        // lenken sender deg til /veterinar/koba-vets.no i stedet for ut av
        // appen. Folk skriver sjelden "https://" for hand.
        Assert.Equal(forventet, Sted(nettside: skrevet).NettsideLenke);
    }

    [Theory]
    [InlineData("https://koba-vets.no")]
    [InlineData("http://koba-vets.no")]
    [InlineData("HTTPS://koba-vets.no")]
    public void Protokoll_som_alt_finnes_beholdes(string skrevet)
    {
        // Skal ikke bli "https://https://…". Store bokstaver teller ogsa.
        Assert.Equal(skrevet, Sted(nettside: skrevet).NettsideLenke);
    }

    [Fact]
    public void Uten_nettside_er_det_ingen_lenke()
        => Assert.Null(Sted().NettsideLenke);

    [Theory]
    [InlineData("55 12 34 56", "55123456")]
    [InlineData("+47 800 12 345", "+4780012345")]
    [InlineData("988 30 889", "98830889")]
    public void Telefonlenken_stripper_alt_annet_enn_siffer_og_pluss(
        string skrevet, string forventet)
    {
        // Mellomrom i en tel:-lenke avvises av enkelte telefoner. Det som
        // VISES beholder formateringen brukeren skrev - det er bare lenken
        // som strippes.
        Assert.Equal(forventet, Sted(telefon: skrevet).TelefonLenke);
    }

    [Fact]
    public void Uten_telefon_er_det_ingen_ringeknapp()
        => Assert.Null(Sted().TelefonLenke);
}
