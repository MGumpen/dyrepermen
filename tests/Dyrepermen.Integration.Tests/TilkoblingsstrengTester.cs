using Dyrepermen.Infrastructure.Persistence;
using Npgsql;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Ren logikk, men den ligger i Infrastructure fordi den bruker Npgsql sin
/// egen bygger - og det er kun dette testprosjektet som ser den. Trenger
/// ingen database, og har derfor ingen samlingsattributt.
///
/// Testene finnes fordi feilen de dekker faktisk stoppet en utrulling:
/// Render fikk en URI, Npgsql kastet i konstruktoren, og appen kom aldri opp.
/// </summary>
public sealed class TilkoblingsstrengTester
{
    private static NpgsqlConnectionStringBuilder Les(string s)
        => new(Tilkoblingsstreng.Normaliser(s));

    [Fact]
    public void Neon_URI_oversettes_til_nokkel_og_verdi()
    {
        var b = Les("postgresql://eier:hemmelig@ep-kald-sky-123.eu-central-1"
                    + ".aws.neon.tech/dyrepermen?sslmode=require");

        Assert.Equal("ep-kald-sky-123.eu-central-1.aws.neon.tech", b.Host);
        Assert.Equal(5432, b.Port);
        Assert.Equal("dyrepermen", b.Database);
        Assert.Equal("eier", b.Username);
        Assert.Equal("hemmelig", b.Password);
        Assert.Equal(SslMode.Require, b.SslMode);
    }

    [Fact]
    public void Nokkel_og_verdi_slippes_gjennom_urort()
    {
        // Lokalt er strengen allerede pa riktig form. Da skal den ikke rores.
        const string lokal = "Host=localhost;Port=5434;Database=dyrepermen;"
                           + "Username=dyrepermen;Password=utvikling";

        Assert.Equal(lokal, Tilkoblingsstreng.Normaliser(lokal));
    }

    [Fact]
    public void Prosentkodet_passord_avkodes()
    {
        // Neon lager passord med tegn som ma kodes i en URI. Uten avkoding
        // sendes "p%40ss" som seks tegn, og paloggingen feiler med en melding
        // om feil passord - som sender feilsokingen helt feil vei.
        var b = Les("postgresql://bruker:p%40ss%3Aord@vert/base");

        Assert.Equal("p@ss:ord", b.Password);
    }

    [Fact]
    public void Port_i_URI_beholdes()
    {
        Assert.Equal(6543, Les("postgres://a:b@vert:6543/base").Port);
    }

    [Fact]
    public void URI_uten_port_far_5432_ikke_minus_en()
    {
        // Uri.Port gir -1 nar porten mangler. Sendes den videre, blir
        // tilkoblingsstrengen ugyldig pa en helt annen mate.
        Assert.Equal(5432, Les("postgres://a:b@vert/base").Port);
    }

    [Fact]
    public void TLS_kreves_selv_om_URI_ikke_sier_noe()
    {
        // En tilkobling i klartekst mot en database pa internett skal ikke
        // kunne oppsta ved at noen glemte en parameter.
        Assert.Equal(SslMode.Require, Les("postgres://a:b@vert/base").SslMode);
    }

    [Fact]
    public void Sslmode_fra_URI_respekteres_nar_den_er_strengere()
    {
        Assert.Equal(
            SslMode.VerifyFull,
            Les("postgres://a:b@vert/base?sslmode=verify-full").SslMode);
    }

    [Fact]
    public void Pooltak_settes_for_Neon()
    {
        // Neon Free tar fa samtidige tilkoblinger. Uten tak apner appen sa
        // mange den vil, og databasen avviser dem under last.
        Assert.Equal(10, Les("postgres://a:b@vert/base").MaxPoolSize);
    }

    [Theory]
    [InlineData("postgres://a:b@vert/base")]
    [InlineData("postgresql://a:b@vert/base")]
    [InlineData("POSTGRESQL://a:b@vert/base")]
    public void Begge_skrivematene_gjenkjennes(string uri)
        => Assert.True(Tilkoblingsstreng.ErUri(uri));

    [Fact]
    public void Nokkel_og_verdi_er_ikke_en_URI()
        => Assert.False(Tilkoblingsstreng.ErUri("Host=localhost;Database=x"));

    [Fact]
    public void Npgsql_avviser_URI_direkte()
    {
        // Dette er hele grunnen til at Normaliser finnes, og det er verdt a
        // pinne: begynner Npgsql en dag a stotte URI-er, feiler denne testen,
        // og da kan laget fjernes i stedet for a bli staende som noe ingen
        // tor rore.
        //
        // Konstruktoren kaster FOR den har forsokt a kontakte noe. Derfor sa
        // utrullingsfeilen pa Render ut som et nettverksproblem.
        Assert.ThrowsAny<Exception>(
            () => new NpgsqlConnection("postgresql://bruker:passord@vert/base"));
    }
}
