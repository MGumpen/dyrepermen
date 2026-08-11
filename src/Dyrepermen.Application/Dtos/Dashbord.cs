namespace Dyrepermen.Application.Dtos;

public sealed record Dashbord(
    IReadOnlyList<DyrKort> Dyr,
    IReadOnlyList<Paminnelse> Forfaller,
    IReadOnlyList<HandlelisteRad> Handleliste);
