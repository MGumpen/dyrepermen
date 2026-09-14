using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

public sealed class ForplanSideVm
{
    public int DyrId { get; set; }

    public string DyrNavn { get; set; } = string.Empty;

    public ForplanResultat Resultat { get; set; } = ForplanResultat.IngenPlan();

    public ForplanRad? Aktiv { get; set; }

    /// <summary>Dagsmengden fordelt pa maltider. Summen av begge fortypene.</summary>
    public int[] Maltider { get; set; } = [];

    /// <summary>
    /// Fordelingen per fortype. Poenget med en blandingsplan er at to ting
    /// skal veies opp, og da holder det ikke a vise summen - da ma brukeren
    /// gjore delingen i hodet ved hvert maltid.
    /// </summary>
    public int[] VektdelMaltider { get; set; } = [];

    public int[] AldersdelMaltider { get; set; } = [];

    /// <summary>
    /// Fodselsdatoen mangler, og torrformengden lar seg ikke sla opp. Da
    /// skal siden peke pa dyresiden, ikke bare si fra.
    /// </summary>
    public bool HarFodselsdato { get; set; }

    /// <summary>
    /// Fornavn husstanden har brukt for. Fyller en datalist pa navnefeltene,
    /// sa "VOM Puppy" skrives inn en gang og velges resten av gangene.
    /// </summary>
    public IReadOnlyList<string> Fornavnforslag { get; set; } = [];

    public NyForplanVm Ny { get; set; } = new();
}
