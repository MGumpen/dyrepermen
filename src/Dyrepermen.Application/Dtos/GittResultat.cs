namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Svaret nar en behandling krysses av som gitt. <see cref="Beskrivelse"/>
/// og <see cref="NesteDato"/> er med slik at bekreftelsen kan si hva som
/// ble registrert - datoen er foreslatt av appen, og brukeren skal se den.
/// </summary>
public sealed record GittResultat(
    Gittstatus Status,
    string? Beskrivelse,
    DateOnly? NesteDato)
{
    public static GittResultat Lagret(string beskrivelse, DateOnly? neste)
        => new(Gittstatus.Lagret, beskrivelse, neste);

    public static GittResultat AlleredeFulgtOpp()
        => new(Gittstatus.AlleredeFulgtOpp, null, null);

    public static GittResultat GittIdag()
        => new(Gittstatus.GittIdag, null, null);

    public static GittResultat FinnesIkke()
        => new(Gittstatus.FinnesIkke, null, null);
}
