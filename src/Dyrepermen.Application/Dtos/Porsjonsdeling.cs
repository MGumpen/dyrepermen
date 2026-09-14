namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Hvor mye av hver fortype som gar i ETT maltid.
///
/// Totalen alene er ubrukelig ved en overgangsplan: den som star ved skalen
/// skal veie opp to ting, og "171 g" sier ikke hvor mye av hver. Da ma hun
/// enten regne selv eller apne forplanen - hver eneste gang.
///
/// Navnene folger med fordi "144 g VOM Puppy" er noe man kan handle og veie
/// etter, mens "144 g rafor" ma oversettes forst.
/// </summary>
public sealed record Porsjonsdeling(
    int VektdelGram,
    string? VektdelNavn,
    int AldersdelGram,
    string? AldersdelNavn)
{
    public int SumGram => VektdelGram + AldersdelGram;
}
