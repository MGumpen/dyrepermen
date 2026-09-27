namespace Dyrepermen.Application.Dtos;

public sealed record MedisinRad(
    int Id,
    string Navn,
    string Dose,
    int IntervallTimer,
    DateOnly StartDato,
    DateOnly? SluttDato,
    DateTimeOffset? SisteDoseTid,
    string? SisteDoseAv,

    /// <summary>Satt nar noen trykket "Avslutt". Se ADR 0017.</summary>
    DateTimeOffset? AvsluttetTid)
{
    /// <summary>Null intervall betyr ved behov - da finnes ingen neste dose.</summary>
    public DateTimeOffset? NesteDoseTidligst
        => IntervallTimer > 0 && SisteDoseTid is { } siste
            ? siste.AddHours(IntervallTimer)
            : null;

    /// <summary>
    /// Avsluttet med knappen, eller sluttdatoen har passert. Sluttdatoen er
    /// til og med, sa en kur som slutter i dag, er fortsatt aktiv i dag.
    /// Samme regel som Medisinfilter.Aktiv i sporringene.
    /// </summary>
    public bool ErAvsluttet(DateOnly idag)
        => AvsluttetTid is not null || SluttDato is { } slutt && slutt < idag;
}
