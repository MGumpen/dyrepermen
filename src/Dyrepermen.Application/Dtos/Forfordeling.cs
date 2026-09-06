namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Hvordan dagsmengden fordeler seg pa de to delene av en tabellplan.
///
/// Vektdelen males i prosent av kroppsvekten, aldersdelen slas opp i
/// forposens tabell. Hva fortypene ER, sier modellen ingenting om - en
/// overgang kan ga begge veier, og mellom hva som helst.
///
/// Ved en ren tabellplan er alt aldersdel og <see cref="VektdelGram"/> er
/// null. Blander planen, star begge her - og da er summen alene ubrukelig:
/// den som star ved skalen skal veie opp to ting.
///
/// Fullmengdene er med fordi de er svaret pa "hvor kommer tallet fra":
/// 287 g gir ingen mening for man ser at 410 g er full mengde og at andelen
/// star pa 70 %.
/// </summary>
public sealed record Forfordeling(
    int VektdelFullGram,
    int VektdelAndelProsent,
    int VektdelGram,
    int AldersdelFullGram,
    int AldersdelGram,
    int AlderUker,
    bool AlderUtenforTabellen = false)
{
    public int AldersdelAndelProsent => 100 - VektdelAndelProsent;

    /// <summary>Sant nar planen faktisk blander to fortyper.</summary>
    public bool Blander => VektdelAndelProsent is > 0 and < 100;
}
