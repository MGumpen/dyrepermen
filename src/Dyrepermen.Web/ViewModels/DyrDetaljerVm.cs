using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

public sealed class DyrDetaljerVm
{
    public DyrDetaljer Dyr { get; set; } = null!;

    public DyrSammendrag Sammendrag { get; set; } = null!;
}
