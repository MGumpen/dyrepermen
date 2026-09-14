using Microsoft.AspNetCore.Identity;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// Utvider Identity med visningsnavn og medlemskap.
///
/// Kolonnen husstand_id er FJERNET. En bruker kan vaere med i flere
/// husstander - egen, og for eksempel farens der hun passer hunden - og da
/// holder ikke en enkeltverdi. Tilknytningen ligger i
/// <see cref="Medlemskap"/>. Se ADR 0009.
///
/// E-post er innloggingsnavnet: <c>UserName</c> og <c>Email</c> settes like
/// ved registrering. Gjores ikke det, feiler innlogging med e-post selv om
/// passordet er riktig. Se plan kapittel 11.2.
/// </summary>
public sealed class Bruker : IdentityUser<int>
{
    public string Visningsnavn { get; set; } = null!;

    public ICollection<Husstandsmedlemskap> Medlemskap { get; set; }
        = new List<Husstandsmedlemskap>();
}
