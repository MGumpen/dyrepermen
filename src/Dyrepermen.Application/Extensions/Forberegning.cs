using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Extensions;

/// <summary>
/// Regnestykket bak formengden - ETT sted.
///
/// Regelen la for i fire kopier: ForplanService, DashbordService,
/// DyrService og Forplanformat. De var like, men bare fordi ingen hadde
/// endret noen av dem enda. En regel som star fire steder, spriker, og da
/// viser dashbordet 53 g mens forplansiden viser 54.
///
/// Ren logikk uten databasetilgang, jf. CLAUDE.md: tjenestene henter
/// grunnlaget, denne klassen regner. Dagens dato sendes inn, sa en 17 uker
/// gammel valp lar seg teste uten a vente til hosten.
/// </summary>
public static class Forberegning
{
    public static ForplanResultat Beregn(
        Forplanregel? regel, Beregningsgrunnlag grunnlag)
    {
        if (regel is null)
        {
            return ForplanResultat.IngenPlan();
        }

        // En gammel rad kan ha kommet inn for valideringen ble strammet, og
        // deling pa null ville tatt ned hele dashbordet - ikke bare kortet.
        var maltider = regel.AntallMaltider <= 0 ? 2 : regel.AntallMaltider;

        return regel.Metode switch
        {
            // Fast mengde star stille til den endres.
            Formetode.Gram => ForplanResultat.Ok(regel.GramPerDag ?? 0, maltider),

            Formetode.Prosent => Prosent(regel, grunnlag, maltider),

            _ => EtterTabell(regel, grunnlag, maltider)
        };
    }

    /// <summary>
    /// prosent_tidels = 50 betyr 5,0 %. Avrunding bort fra null, ikke til
    /// partall: 410,5 blir 411.
    /// </summary>
    public static int AvVekt(int vektGram, int prosentTidels)
        => (int)Math.Round(
            vektGram * prosentTidels / 1000.0, MidpointRounding.AwayFromZero);

    private static ForplanResultat Prosent(
        Forplanregel regel, Beregningsgrunnlag grunnlag, int maltider)
    {
        // IKKE 0 gram. Uten vektgrunnlag har tallet ingen mening, og et tall
        // uten dekning er verre enn ingen tall.
        if (grunnlag.SisteVektGram is not { } vekt)
        {
            return ForplanResultat.ManglerVektgrunnlag();
        }

        return ForplanResultat.Ok(
            AvVekt(vekt, regel.ProsentTidels ?? 0),
            maltider,
            vekt,
            grunnlag.SisteVektDato);
    }

    /// <summary>
    /// Tabellmetoden: mengden slas opp pa alder, og kan i tillegg blandes
    /// med et for som males i prosent av kroppsvekten.
    ///
    /// Retningen er ikke bestemt. Det kan vaere en valp som trappes fra
    /// rafor over pa torrfor, eller motsatt vei, eller to torrfor der det
    /// ene har en alderstabell og det andre ikke. Delene heter derfor det de
    /// er: en vektdel og en aldersdel.
    ///
    /// **Andelen skalerer hver del for seg**, ikke en felles dagsmengde.
    /// 70 % vektdel betyr 70 % av det vektregelen gir, pluss 30 % av det
    /// tabellen gir. To fortyper er sjelden sammenlignbare gram for gram -
    /// torrfor er torket til om lag en tredel av vekten - sa en felles
    /// dagsmengde delt 70/30 ville gitt et dyr som gikk ned i vekt gjennom
    /// overgangen. Se ADR 0012.
    ///
    /// Star andelen pa 0, er dette en ren tabellplan: ingen vekt trengs, og
    /// ingen prosentsats.
    /// </summary>
    private static ForplanResultat EtterTabell(
        Forplanregel regel, Beregningsgrunnlag grunnlag, int maltider)
    {
        var vektdelAndel = Math.Clamp(regel.VektdelAndelProsent ?? 0, 0, 100);
        var aldersAndel = 100 - vektdelAndel;

        // Grunnlaget kreves kun for den delen som faktisk brukes. En ren
        // tabellplan skal ikke stoppe pa en manglende vekt, og en plan uten
        // aldersdel skal ikke stoppe pa en manglende fodselsdato.
        if (vektdelAndel > 0 && grunnlag.SisteVektGram is null)
        {
            return ForplanResultat.ManglerVektgrunnlag();
        }

        if (aldersAndel > 0 && grunnlag.Fodselsdato is null)
        {
            return ForplanResultat.ManglerAldersgrunnlag();
        }

        var vektdelFull = grunnlag.SisteVektGram is { } vekt
            ? AvVekt(vekt, regel.ProsentTidels ?? 0)
            : 0;

        var uker = grunnlag.Fodselsdato is { } fodt
            ? Alderformat.UkerSiden(fodt, grunnlag.Idag)
            : 0;

        var oppslag = Fortabell.Slaopp(regel.Tabelltrinn, uker);

        var fordeling = new Forfordeling(
            vektdelFull,
            vektdelAndel,
            Andel(vektdelFull, vektdelAndel),
            oppslag.Gram,
            Andel(oppslag.Gram, aldersAndel),
            uker,
            // Flagget gjelder bare nar tabellen faktisk er i bruk.
            aldersAndel > 0 && oppslag.UtenforTabellen);

        return ForplanResultat.OkFordelt(
            maltider,
            fordeling,
            // Vektgrunnlaget vises kun nar det faktisk er brukt.
            vektdelAndel > 0 ? grunnlag.SisteVektGram : null,
            vektdelAndel > 0 ? grunnlag.SisteVektDato : null);
    }

    private static int Andel(int gram, int prosent)
        => (int)Math.Round(gram * prosent / 100.0, MidpointRounding.AwayFromZero);
}
