namespace Dyrepermen.Application.Dtos;

public sealed record MedisinRad(
    int Id,
    string Navn,
    string Dose,
    int IntervallTimer,
    DateOnly StartDato,
    DateOnly? SluttDato,
    DateTimeOffset? SisteDoseTid,
    string? SisteDoseAv)
{
    /// <summary>Null intervall betyr ved behov - da finnes ingen neste dose.</summary>
    public DateTimeOffset? NesteDoseTidligst
        => IntervallTimer > 0 && SisteDoseTid is { } siste
            ? siste.AddHours(IntervallTimer)
            : null;

    public bool ErAvsluttet(DateOnly idag)
        => SluttDato is { } slutt && slutt < idag;
}
