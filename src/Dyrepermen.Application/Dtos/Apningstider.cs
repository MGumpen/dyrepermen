namespace Dyrepermen.Application.Dtos;

/// <summary>En dag som er apen, med tiden slik brukeren skrev den.</summary>
/// <param name="Dag">"Mandag". Brukes der det er plass.</param>
/// <param name="Kortdag">"Man". Brukes i lister der raden skal vaere smal.</param>
/// <param name="Tid">"08-16" eller "10-14, 16-20".</param>
public sealed record Apningsdag(string Dag, string Kortdag, string Tid);

/// <summary>
/// Apningstid per ukedag, som fritekst.
///
/// Fritekst og ikke fra/til-klokkeslett fordi virkeligheten ikke lar seg
/// presse inn i to tidspunkter: "10-14, 16-20" er vanlig hos veterinaerer, og
/// vakta er ofte "Dognapent". To klokkeslett ville tvunget slike tilfeller ut
/// i notatfeltet, der ingen leter etter dem.
///
/// En tom dag betyr stengt. Det er derfor ingen egen "stengt"-verdi - da
/// ville det finnes to mater a si det samme pa.
/// </summary>
public sealed record Apningstider(
    string? Mandag,
    string? Tirsdag,
    string? Onsdag,
    string? Torsdag,
    string? Fredag,
    string? Lordag,
    string? Sondag)
{
    public static Apningstider Tom { get; } =
        new(null, null, null, null, null, null, null);

    /// <summary>
    /// Dagene som faktisk har en tid, i ukerekkefolge.
    ///
    /// Rekkefolgen og dagsnavnene ligger HER og ikke i visningene. Listen
    /// tegnes tre steder - veterinaersiden, detaljkortet og dashbordet - og
    /// tre kopier av samme rekkefolge er tre steder den kan komme i utakt.
    ///
    /// Ukestart er mandag, ikke sondag. DayOfWeek i .NET begynner pa sondag,
    /// og hadde rekkefolgen kommet derfra, ville sondag statt oeverst.
    /// </summary>
    public IReadOnlyList<Apningsdag> Utfylte =>
        new (string Dag, string Kort, string? Tid)[]
        {
            ("Mandag", "Man", Mandag),
            ("Tirsdag", "Tir", Tirsdag),
            ("Onsdag", "Ons", Onsdag),
            ("Torsdag", "Tor", Torsdag),
            ("Fredag", "Fre", Fredag),
            ("Lørdag", "Lør", Lordag),
            ("Søndag", "Søn", Sondag)
        }
        .Where(d => !string.IsNullOrWhiteSpace(d.Tid))
        .Select(d => new Apningsdag(d.Dag, d.Kort, d.Tid!.Trim()))
        .ToList();

    /// <summary>True nar minst en dag er fylt ut.</summary>
    public bool Finnes => Utfylte.Count > 0;
}
