using System.Net;
using System.Text.RegularExpressions;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Sender skjemaer slik en nettleser gjor det: henter siden, plukker
/// antiforgery-tokenet, og poster feltene med informasjonskapslene i behold.
///
/// Uten tokenet svarer serveren 400, og en test som bare sjekker "ikke 200"
/// ville da bestatt av feil grunn.
/// </summary>
public sealed partial class Skjemaklient
{
    private readonly HttpClient _klient;

    public Skjemaklient(HttpClient klient) => _klient = klient;

    [GeneratedRegex(
        """name="__RequestVerificationToken"[^>]*value="([^"]+)""" + "\"")]
    private static partial Regex Tokenmonster();

    /// <summary>
    /// Vanlig GET med informasjonskapslene i behold. Klienten folger ikke
    /// omdirigeringer, sa en side som krever innlogging svarer 302 - og en
    /// test som venter 200, sier fra i stedet for a lese innloggingssiden
    /// som om det var innholdet den ba om.
    /// </summary>
    public Task<HttpResponseMessage> Hent(string sti) => _klient.GetAsync(sti);

    public async Task<string> HentToken(string sti)
    {
        var html = await _klient.GetStringAsync(sti);
        var treff = Tokenmonster().Match(html);

        Assert.True(treff.Success, $"Fant ikke antiforgery-token pa {sti}");
        return treff.Groups[1].Value;
    }

    /// <summary>Henter siden, legger ved tokenet, og poster.</summary>
    public Task<HttpResponseMessage> Post(
        string sti, Dictionary<string, string> felter)
        => Post(sti, felter, tokenFra: sti);

    /// <summary>
    /// Samme, men der tokenet hentes fra en ANNEN side enn den det postes
    /// til. Trengs nar handlingen har sin egen rute: skjemaet star pa
    /// /husstand/oppsett og postes til /husstand/opprett, og en GET mot
    /// sistnevnte finnes ikke.
    /// </summary>
    public async Task<HttpResponseMessage> Post(
        string sti, Dictionary<string, string> felter, string tokenFra)
    {
        var felterMedToken = new Dictionary<string, string>(felter)
        {
            ["__RequestVerificationToken"] = await HentToken(tokenFra)
        };

        return await _klient.PostAsync(sti, new FormUrlEncodedContent(felterMedToken));
    }

    /// <summary>
    /// True nar svaret er en omdirigering, altsa at handlingen gikk gjennom.
    /// Et skjema som avvises svarer 200 med seg selv og feilmeldingene.
    /// </summary>
    public static bool GikkGjennom(HttpResponseMessage svar)
        => svar.StatusCode is HttpStatusCode.Found or HttpStatusCode.Redirect;

    /// <summary>Feilmeldingene i valideringsoppsummeringen, som ren tekst.</summary>
    public static async Task<string> Feilmeldinger(HttpResponseMessage svar)
    {
        var html = await svar.Content.ReadAsStringAsync();
        var boks = Regex.Match(
            html, """<div[^>]*validation-summary[^>]*>(.*?)</div>""",
            RegexOptions.Singleline);

        var raa = boks.Success
            ? boks.Groups[1].Value
            : Regex.Match(html, """<div class="alert alert-danger[^"]*"[^>]*>(.*?)</div>""",
                RegexOptions.Singleline).Groups[1].Value;

        return WebUtility.HtmlDecode(
            Regex.Replace(Regex.Replace(raa, "<[^>]+>", " "), @"\s+", " ")).Trim();
    }
}
