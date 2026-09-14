using Dyrepermen.Domain.Abstractions;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// En veterinaertime - kommende eller gjennomfort.
///
/// Det er ingen statuskolonne. En time er kommende sa lenge datoen ikke har
/// passert, og gjennomfort etterpa. En egen status ville krevd at noen huket
/// av etter hvert besok, og den som glemte det ville sittet med en "kommende"
/// time fra i fjor.
/// </summary>
public sealed class Vetbesok : IHusstandsbundet
{
    public int Id { get; set; }

    public int DyrId { get; set; }

    public Dyr Dyr { get; set; } = null!;

    /// <summary>
    /// Stedet, nar det star i listen over veterinaerer. Nullbar fordi man
    /// ogsa oppsoker steder man ikke har lagret - pa reise, eller den ene
    /// gangen det hastet.
    /// </summary>
    public int? VeterinarId { get; set; }

    public Veterinar? Veterinar { get; set; }

    public DateOnly Dato { get; set; }

    /// <summary>
    /// Kun for kommende timer. Et besok i fjor trenger ikke klokkeslett, og
    /// a kreve det ville gjort etterregistrering til gjettearbeid.
    /// </summary>
    public TimeOnly? Klokkeslett { get; set; }

    /// <summary>
    /// Fritekst for steder som ikke star i listen. Star VeterinarId, brukes
    /// navnet derfra - se Vetbesokrad.Sted.
    /// </summary>
    public string? Klinikk { get; set; }

    public string Arsak { get; set; } = null!;

    public string? Diagnose { get; set; }

    /// <summary>
    /// Nullbar. En kommende time har ingen pris enna, og 0 ville bety at
    /// besoket var gratis. Se plan kapittel 10.3: et tall uten dekning er
    /// verre enn ingen tall.
    /// </summary>
    public int? KostnadKr { get; set; }

    public bool ForsikringKrevd { get; set; }

    /// <summary>Hva forsikringen faktisk dekket, i hele kroner.</summary>
    public int? RefundertKr { get; set; }

    /// <summary>
    /// Avtalt oppfolging. Dukker opp i "Forfaller snart" pa dashbordet, sa
    /// kontrollen om tre uker ikke blir glemt.
    /// </summary>
    public DateOnly? NesteKontrollDato { get; set; }

    public string? Notat { get; set; }
}
