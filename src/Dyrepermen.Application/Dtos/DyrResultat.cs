namespace Dyrepermen.Application.Dtos;

public sealed record DyrResultat(bool Ok, int DyrId, DyrFeil Feil)
{
    public static DyrResultat Lagret(int dyrId) => new(true, dyrId, DyrFeil.Ingen);

    public static DyrResultat Avvist(DyrFeil feil) => new(false, 0, feil);
}
