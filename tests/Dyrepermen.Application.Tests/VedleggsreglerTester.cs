using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Hvitlisten fra plan kapittel 15: pdf, jpg og png, hoyst 10 MB. Typen
/// avgjores av de forste bytene i filen, ikke av filnavnet - en .exe som
/// heter kvittering.jpg skal ikke slippe gjennom.
/// </summary>
public sealed class VedleggsreglerTester
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00];
    private static readonly byte[] Pdf = "%PDF-1.7\n"u8.ToArray();

    [Fact]
    public void Jpeg_png_og_pdf_gjenkjennes_pa_innholdet()
    {
        Assert.Equal("image/jpeg", Vedleggsregler.Innholdstype(Jpeg));
        Assert.Equal("image/png", Vedleggsregler.Innholdstype(Png));
        Assert.Equal("application/pdf", Vedleggsregler.Innholdstype(Pdf));
    }

    [Theory]
    [InlineData(new byte[] { 0x4D, 0x5A, 0x90, 0x00 })]          // Windows-program
    [InlineData(new byte[] { 0x3C, 0x73, 0x76, 0x67, 0x3E })]    // <svg> - kan inneholde skript
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 })] // GIF
    [InlineData(new byte[] { 0xFF, 0xD8 })]                       // for kort til a vaere en jpeg
    [InlineData(new byte[0])]
    public void Alt_annet_avvises(byte[] data)
    {
        Assert.Null(Vedleggsregler.Innholdstype(data));
    }

    [Fact]
    public void Filnavnet_avgjor_ikke_typen()
    {
        var feil = Vedleggsregler.Feil([new NyttVedlegg("kvittering.jpg", [0x4D, 0x5A, 0x90, 0x00])]);

        Assert.Equal(
            "«kvittering.jpg» er ikke et bilde eller en PDF. Bare jpg, png og pdf kan legges ved.",
            feil);
    }

    [Fact]
    public void For_stor_fil_avvises()
    {
        var data = new byte[Vedleggsregler.MaksFilByte + 1];
        Jpeg.CopyTo(data, 0);

        Assert.Equal(
            "«stor.jpg» er for stor. En fil kan være høyst 10 MB.",
            Vedleggsregler.Feil([new NyttVedlegg("stor.jpg", data)]));
    }

    [Fact]
    public void Akkurat_pa_grensen_godtas()
    {
        var data = new byte[Vedleggsregler.MaksFilByte];
        Pdf.CopyTo(data, 0);

        Assert.Null(Vedleggsregler.Feil([new NyttVedlegg("grense.pdf", data)]));
    }

    [Fact]
    public void Tom_fil_avvises()
    {
        Assert.Equal("«tom.pdf» er tom.",
            Vedleggsregler.Feil([new NyttVedlegg("tom.pdf", [])]));
    }

    [Fact]
    public void Gyldige_filer_gir_ingen_feil()
    {
        Assert.Null(Vedleggsregler.Feil(
        [
            new NyttVedlegg("side1.jpg", Jpeg),
            new NyttVedlegg("side2.png", Png),
            new NyttVedlegg("epost.pdf", Pdf)
        ]));
    }

    [Theory]
    [InlineData(@"C:\Users\marius\Pictures\kvittering.jpg", "kvittering.jpg")]
    [InlineData("/home/marius/kvittering.jpg", "kvittering.jpg")]
    [InlineData("  kvittering.jpg  ", "kvittering.jpg")]
    [InlineData("", "vedlegg")]
    [InlineData("   ", "vedlegg")]
    public void Navnet_renses_for_sti_og_mellomrom(string inn, string ut)
    {
        Assert.Equal(ut, Vedleggsregler.Navn(inn));
    }

    [Fact]
    public void Langt_navn_kuttes_til_kolonnens_lengde()
    {
        Assert.Equal(Vedleggsregler.MaksNavnLengde,
            Vedleggsregler.Navn(new string('a', 500) + ".jpg").Length);
    }

    [Fact]
    public void Profilbildet_ma_vaere_et_bilde()
    {
        Assert.Null(Vedleggsregler.ProfilbildeFeil(new NyttVedlegg("luna.jpg", Jpeg)));
        Assert.Null(Vedleggsregler.ProfilbildeFeil(new NyttVedlegg("luna.png", Png)));
        Assert.Equal(
            "«luna.pdf» er ikke et bilde. Profilbildet må være jpg eller png.",
            Vedleggsregler.ProfilbildeFeil(new NyttVedlegg("luna.pdf", Pdf)));
    }

    [Fact]
    public void Profilbildet_folger_de_vanlige_reglene_forst()
    {
        // En fil som ikke er noe av det godkjente, far den vanlige meldingen -
        // ikke "ikke et bilde", som ville antydet at en PDF hadde gatt.
        Assert.Equal(
            "«luna.jpg» er ikke et bilde eller en PDF. Bare jpg, png og pdf kan legges ved.",
            Vedleggsregler.ProfilbildeFeil(new NyttVedlegg("luna.jpg", [0x4D, 0x5A, 0x90, 0x00])));
    }
}
