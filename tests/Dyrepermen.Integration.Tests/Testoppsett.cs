using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Integration.Tests;

/// <summary>
/// Stegene som ma til for at en test kan hente en vanlig side over HTTP:
/// registrere en bruker, opprette husstanden, og legge inn et dyr.
///
/// Ligger her og ikke som private metoder i hver testklasse fordi to klasser
/// na trenger dem. Registreringen alene er tre ikke-opplagte detaljer - at
/// husstanden ma opprettes for noe som helst svarer 200, at tokenet hentes
/// fra en annen side enn den det postes til, og at id-en leses ut av
/// omdirigeringen.
/// </summary>
internal static class Testoppsett
{
    /// <summary>
    /// Registrerer en fersk bruker med egen husstand, og returnerer klienten
    /// hennes innlogget.
    ///
    /// Begge stegene trengs. Registreringen oppretter kontoen og logger inn,
    /// men uten invitasjon lander brukeren pa oppsettsiden - resten av appen
    /// er stengt til husstanden finnes, og en test som hopper over steg to
    /// far 302 i stedet for siden den skulle lest.
    /// </summary>
    public static async Task<Skjemaklient> InnloggetKlient(Appfabrikk app)
    {
        var klient = new Skjemaklient(app.LagKlient());

        var registrert = await klient.Post("/registrer", new Dictionary<string, string>
        {
            // Unik adresse per test, sa rekkefolgen aldri spiller inn.
            ["Epost"] = $"test-{Guid.NewGuid():N}@example.test",
            ["Visningsnavn"] = "Testbruker",
            ["Passord"] = "Passord123",
            ["BekreftPassord"] = "Passord123"
        });

        Assert.True(
            Skjemaklient.GikkGjennom(registrert),
            $"Registreringen feilet: {await Skjemaklient.Feilmeldinger(registrert)}");

        // Skjemaet star pa /husstand/oppsett og postes til /husstand/opprett.
        var opprettet = await klient.Post(
            "/husstand/opprett",
            new Dictionary<string, string> { ["Navn"] = "Testhusstanden" },
            tokenFra: "/husstand/oppsett");

        Assert.True(
            Skjemaklient.GikkGjennom(opprettet),
            $"Husstanden ble ikke opprettet: {await Skjemaklient.Feilmeldinger(opprettet)}");

        return klient;
    }

    /// <summary>Oppretter et dyr og returnerer id-en.</summary>
    public static async Task<int> NyttDyr(Skjemaklient klient, string navn = "Luna")
    {
        var lagret = await klient.Post("/dyr/ny", new Dictionary<string, string>
        {
            ["Navn"] = navn,
            // Tallverdien, ikke navnet. Skjemaet bruker GetEnumSelectList, som
            // skriver ut <option value="0">Hund</option> - postes "Hund",
            // binder ikke feltet.
            ["Art"] = ((int)Art.Hund).ToString(),
            ["Kjonn"] = ((int)Kjonn.Tispe).ToString()
        });

        Assert.True(
            Skjemaklient.GikkGjennom(lagret),
            $"Dyret ble ikke lagret: {await Skjemaklient.Feilmeldinger(lagret)}");

        // Id-en star i omdirigeringen: /dyr/{id}.
        return int.Parse(lagret.Headers.Location!.ToString().Split('/').Last());
    }

    /// <summary>
    /// Slar pa foringsloggen for dyret.
    ///
    /// Uten den svarer /dyr/{id}/foring 404 - bryteren av betyr at siden ikke
    /// finnes, samme svar som for et dyr i en annen husstand.
    ///
    /// Begge bryterne ma postes. Skjemaet sender begge, og utelates en av dem
    /// binder den til false: da skrur man av forplanen mens man skrur pa
    /// loggen, uten a ha bedt om det.
    /// </summary>
    public static async Task SlaPaForingslogg(
        Skjemaklient klient, int dyrId, string navn = "Luna")
    {
        var svar = await klient.Post(
            $"/dyr/{dyrId}/rediger",
            new Dictionary<string, string>
            {
                ["Navn"] = navn,
                ["Art"] = ((int)Art.Hund).ToString(),
                ["Kjonn"] = ((int)Kjonn.Tispe).ToString(),
                ["ForingsloggAktiv"] = "true",
                ["ForplanAktiv"] = "true"
            },
            tokenFra: $"/dyr/{dyrId}/rediger");

        Assert.True(
            Skjemaklient.GikkGjennom(svar),
            $"Kunne ikke sla pa foringsloggen: {await Skjemaklient.Feilmeldinger(svar)}");
    }

    /// <summary>Gir dyret en forplan i gram, sa porsjonen ikke trenger vekt.</summary>
    public static async Task ForplanIGram(
        Skjemaklient klient, int dyrId, int gramPerDag, int antallMaltider)
    {
        var plan = await klient.Post(
            $"/dyr/{dyrId}/forplan",
            new Dictionary<string, string>
            {
                ["Metode"] = ((int)Formetode.Gram).ToString(),
                ["GramPerDag"] = gramPerDag.ToString(),
                ["AntallMaltider"] = antallMaltider.ToString()
            },
            tokenFra: $"/dyr/{dyrId}/forplan");

        Assert.True(
            Skjemaklient.GikkGjennom(plan),
            $"Forplanen ble ikke lagret: {await Skjemaklient.Feilmeldinger(plan)}");
    }
}
