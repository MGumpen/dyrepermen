using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

public sealed class VektSideVm
{
    public int DyrId { get; set; }

    public string DyrNavn { get; set; } = string.Empty;

    public IReadOnlyList<VektRad> Historikk { get; set; } = [];

    /// <summary>Null ved faerre enn to malinger - da er det ingen graf.</summary>
    public Vektgrafdata? Graf { get; set; }

    public NyVektVm Ny { get; set; } = new();
}
