using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.ViewModels;

/// <summary>
/// Veterinaersiden: stedene oeverst, timene under. To lister, samme side -
/// de svarer pa hvert sitt sporsmal, men man er der av samme grunn.
/// </summary>
public sealed class VeterinarSideVm
{
    public IReadOnlyList<Veterinarrad> Steder { get; init; } = [];

    /// <summary>Time i dag eller senere, tidligst forst.</summary>
    public IReadOnlyList<Vetbesokrad> Kommende { get; init; } = [];

    /// <summary>Gjennomforte besok, nyeste forst.</summary>
    public IReadOnlyList<Vetbesokrad> Tidligere { get; init; } = [];

    /// <summary>Sum av det husstanden har betalt selv, innevaerende ar.</summary>
    public int BetaltIArKr { get; init; }

    public int RefundertIArKr { get; init; }
}
