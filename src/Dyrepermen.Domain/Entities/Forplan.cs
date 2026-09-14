using Dyrepermen.Domain.Abstractions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Brukerdefinert forregel per dyr.
///
/// Applikasjonen anbefaler ikke formengde - den regner ut regelen brukeren
/// selv har lagt inn. Riktig mengde avhenger av art, rase, alder, fortype,
/// aktivitetsniva og hold, og en innebygd formel ville gitt et tall som ser
/// autoritativt ut uten a ha dekning for det. Se plan kapittel 8.1.
///
/// De tre metodene er gjensidig utelukkende, handhevet av ck_forplan_verdi.
/// Kun en plan per dyr kan vaere aktiv, handhevet av ux_forplan_aktiv.
/// </summary>
public sealed class Forplan : IHusstandsbundet
{
    public int Id { get; set; }

    public int DyrId { get; set; }

    public Dyr Dyr { get; set; } = null!;

    public Formetode Metode { get; set; }

    /// <summary>
    /// Tidels prosent av kroppsvekten: 50 betyr 5,0 %. Holder hele modellen
    /// pa heltall.
    ///
    /// Brukes av Prosent, og av Tabell nar planen blander inn et for som
    /// males etter vekt - da er dette regelen for den delen, og andelen
    /// skalerer den ned. Null ved Gram, og ved en ren tabellplan.
    /// </summary>
    public int? ProsentTidels { get; set; }

    /// <summary>Kun ved metode Gram, ellers null.</summary>
    public int? GramPerDag { get; set; }

    /// <summary>
    /// Hvor stor andel av foret som males etter VEKT, i hele prosent. Resten
    /// males etter alderstabellen. Kun ved metode Tabell, ellers null.
    ///
    /// 0 betyr en ren tabellplan - alt males etter alder. 100 betyr at
    /// tabellen star ubrukt og alt males etter vekt. Begge er lovlige, og
    /// begge er endepunkter i en forovergang.
    ///
    /// **Andelen gjelder hver del for seg**, ikke en felles dagsmengde:
    /// 70 betyr 70 % av den mengden vektregelen gir, pluss 30 % av den
    /// mengden tabellen gir. To fortyper er sjelden sammenlignbare gram for
    /// gram - torrfor er torket ned til om lag en tredel av vekten sin - sa
    /// a dele en felles dagsmengde 70/30 ville gitt feil mengde. Se ADR 0012.
    ///
    /// Hele prosent, ikke tidels som ProsentTidels. Andelen justeres for hand
    /// gjennom en overgang som varer noen uker, og et desimaltegn i det feltet
    /// ville vaert presisjon ingen har bruk for.
    /// </summary>
    public int? VektdelAndelProsent { get; set; }

    public int AntallMaltider { get; set; } = 2;

    /// <summary>
    /// Navn pa foret. Blander en tabellplan to fortyper, er dette det som
    /// males etter vekt.
    /// </summary>
    public string? Fornavn { get; set; }

    /// <summary>
    /// Navn pa foret som males etter alderstabellen. Kun ved metode Tabell.
    /// </summary>
    public string? FornavnAlder { get; set; }

    public string? Notat { get; set; }

    public bool Aktiv { get; set; } = true;

    public DateOnly OpprettetDato { get; set; }

    /// <summary>
    /// Null til planen er redigert forste gang.
    ///
    /// En plan kan bade erstattes og redigeres. Erstattes den, star
    /// opprettelsesdatoen igjen pa den gamle raden og forteller sannheten.
    /// Redigeres den, ville "lagt inn 6. sep" blitt staende over et innhold
    /// fra en helt annen dag. Se ADR 0013.
    /// </summary>
    public DateOnly? EndretDato { get; set; }

    /// <summary>
    /// Alderstabellen fra forposen. Tom for alle andre metoder enn Tabell.
    /// </summary>
    public ICollection<Forplantrinn> Tabelltrinn { get; set; } = new List<Forplantrinn>();
}
