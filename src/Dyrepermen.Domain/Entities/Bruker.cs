using Microsoft.AspNetCore.Identity;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Utvider Identity med husstandstilknytning og visningsnavn.
///
/// <see cref="HusstandId"/> er nullbar inntil brukeren er tilknyttet en
/// husstand. En bruker uten husstand ser ingenting og sendes til
/// /husstand/oppsett av middlewaren. Se plan kapittel 12.1.
///
/// E-post er innloggingsnavnet: <c>UserName</c> og <c>Email</c> settes like
/// ved registrering. Gjores ikke det, feiler innlogging med e-post selv om
/// passordet er riktig. Se plan kapittel 11.2.
/// </summary>
public sealed class Bruker : IdentityUser<int>
{
    public int? HusstandId { get; set; }

    public Husstand? Husstand { get; set; }

    public string Visningsnavn { get; set; } = null!;
}
