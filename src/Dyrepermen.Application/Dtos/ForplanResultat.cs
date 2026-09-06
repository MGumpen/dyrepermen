namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Tilstandene grensesnittet ma handtere er ulike, og de skilles her framfor
/// a la visningen gjette pa null og nuller. Se plan kapittel 8.1.
///
/// <see cref="ManglerVekt"/> er den viktige: en prosentplan uten
/// vektregistrering skal si "Registrer en vekt", ikke vise 0 gram.
/// <see cref="ManglerFodselsdato"/> er den samme regelen for torrforet i en
/// blandingsplan - uten alder finnes det ikke noe oppslag a gjore.
/// </summary>
public sealed record ForplanResultat(
    bool HarPlan,
    bool ManglerVekt,
    int GramPerDag,
    int AntallMaltider,
    int? GrunnlagVektGram,
    DateOnly? GrunnlagDato,
    bool ManglerFodselsdato = false,
    Forfordeling? Fordeling = null)
{
    public static ForplanResultat IngenPlan()
        => new(false, false, 0, 0, null, null);

    public static ForplanResultat ManglerVektgrunnlag()
        => new(true, true, 0, 0, null, null);

    /// <summary>
    /// Torrforet i en blandingsplan leses ut av alderen. Uten fodselsdato er
    /// det ingenting a sla opp, og samme regel gjelder som for vekten: si
    /// fra, ikke vis 0 gram.
    /// </summary>
    public static ForplanResultat ManglerAldersgrunnlag()
        => new(true, false, 0, 0, null, null, ManglerFodselsdato: true);

    /// <summary>Sant nar planen finnes, men mangler noe a regne fra.</summary>
    public bool ManglerGrunnlag => ManglerVekt || ManglerFodselsdato;

    /// <summary>
    /// Gram til ETT maltid. Regelen bor kun finnes her: viser dashbordet 53 g
    /// mens loggen skriver 54, er det ingen som stoler pa noen av tallene.
    ///
    /// Resten fordeles ikke utover maltidene. Med 160 g pa tre blir det
    /// 53 + 53 + 53, ikke 53 + 53 + 54 - ett gram kattemat er under
    /// maleusikkerheten til et kjokkenmal, og en presisjon vi ikke har er
    /// verre enn ingen.
    /// </summary>
    public int PorsjonGram => Fordeling is null
        ? PerMaltid(GramPerDag)
        // Ved blanding er porsjonen summen av de to delene, ikke
        // dagsmengden delt pa antall maltider. De to kan skille ett gram, og
        // et kort som viser "5 g" over "3 g rafor + 3 g torrfor" ser ut som
        // en regnefeil - fordi det er en.
        : VektdelPorsjonGram + AldersdelPorsjonGram;

    /// <summary>Rafor til ETT maltid. Null gram nar planen ikke blander.</summary>
    public int VektdelPorsjonGram => PerMaltid(Fordeling?.VektdelGram ?? 0);

    /// <summary>Torrfor til ETT maltid. Null gram nar planen ikke blander.</summary>
    public int AldersdelPorsjonGram => PerMaltid(Fordeling?.AldersdelGram ?? 0);

    private int PerMaltid(int gram) => AntallMaltider <= 0
        ? gram
        : (int)Math.Round(
            gram / (double)AntallMaltider, MidpointRounding.AwayFromZero);

    public static ForplanResultat Ok(
        int gram,
        int maltider,
        int? grunnlagVektGram = null,
        DateOnly? grunnlagDato = null)
        => new(true, false, gram, maltider, grunnlagVektGram, grunnlagDato);

    /// <summary>
    /// Dagsmengden er summen av de to fortypene. Dashbordet og
    /// foringsskjemaet trenger ett tall a vise og a fylle inn, og summen er
    /// det eneste tallet som er sant for begge. Oppdelingen ligger i
    /// <see cref="Fordeling"/> for de stedene som har plass til den.
    /// </summary>
    public static ForplanResultat OkFordelt(
        int maltider,
        Forfordeling blanding,
        int? grunnlagVektGram = null,
        DateOnly? grunnlagDato = null)
        => new(
            true, false,
            blanding.VektdelGram + blanding.AldersdelGram, maltider,
            grunnlagVektGram, grunnlagDato,
            ManglerFodselsdato: false,
            Fordeling: blanding);
}
