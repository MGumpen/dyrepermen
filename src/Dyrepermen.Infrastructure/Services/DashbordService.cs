using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Application.Interfaces;
using Dyrepermen.Domain.Enums;
using Dyrepermen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Dyrepermen.Infrastructure.Services;

public sealed class DashbordService : IDashbordService
{
    private const int Varselvindu = 14;
    /// <summary>Deles med HjemController, som tegner samme liste pa nytt.</summary>
    public const int AntallPaHandleliste = 5;

    private readonly DyrepermenDbContext _db;
    private readonly IHandlelisteService _handleliste;

    public DashbordService(
        DyrepermenDbContext db, IHandlelisteService handleliste)
    {
        _db = db;
        _handleliste = handleliste;
    }

    public async Task<Dashbord> Hent(CancellationToken ct)
    {
        // Dagens dato i Norge, ikke i UTC. Torrformengden i en blandingsplan
        // folger alderen i hele uker, og en dags avvik flytter hele ukeskiftet.
        var naa = DateTimeOffset.UtcNow;
        var idag = Tidssone.Idag(naa);
        var aktivMedisin = Medisinfilter.Aktiv(idag);
        var grense = idag.AddDays(Varselvindu);

        // Midnatt i Norge, ikke i UTC. Ellers nullstilles maltidstelleren
        // klokka to om natten - etter at kvelden er over.
        var dagStart = Tidssone.DagStart(naa);

        // Sporring 1. Siste vekt, aktiv forplan, neste behandling og aktive
        // medisiner hentes som korrelerte undersporringer inne i Select.
        // Npgsql oversetter dem til LEFT JOIN LATERAL og kjorer alt i samme
        // rundtur - ogsa med tjue dyr.
        //
        // Bruk aldri Include etterfulgt av .Last() i C#: da hentes hele
        // vekthistorikken for hvert dyr, og kravet om fire sporringer ryker
        // pa den forste. Se plan kapittel 10.3.
        var raa = await _db.Dyr
            .OrderBy(d => d.Navn)
            .Select(d => new
            {
                d.Id,
                d.Navn,
                d.Art,
                d.Fodselsdato,
                d.ForingsloggAktiv,
                d.ForplanAktiv,

                // Id-en, ikke bildet. Selve filen hentes av nettleseren via
                // /dokument/{id}, og koster ingenting i denne rundturen.
                ProfilbildeId = d.Dokumenter
                    .Where(x => x.Kategori == DokumentKategori.Profilbilde)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefault(),

                SisteVekt = d.Vekter
                    .OrderByDescending(v => v.Dato)
                    .ThenByDescending(v => v.Id)
                    .Select(v => new { v.VektGram, v.Dato })
                    .FirstOrDefault(),

                Forplan = d.Forplaner
                    .Where(f => f.Aktiv)
                    .Select(f => new
                    {
                        f.Id,
                        f.Metode,
                        f.ProsentTidels,
                        f.GramPerDag,
                        f.AntallMaltider,
                        f.VektdelAndelProsent,
                        f.Fornavn,
                        f.FornavnAlder
                    })
                    .FirstOrDefault(),

                // Kun paminnelser som ikke er fulgt opp. Uten filteret sto
                // forrige ormekur som "neste" lenge etter at den nye var gitt.
                Neste = d.Behandlinger
                    .AsQueryable()
                    .Where(Behandlingsfilter.ApenPaminnelse)
                    .OrderBy(b => b.NesteDato)
                    .Select(b => new { b.Type, b.Preparat, Dato = b.NesteDato!.Value })
                    .FirstOrDefault(),

                // Med intervall og siste dose, slik at dosene som forfaller i
                // dag kan bygges herfra. Det sparer en egen rundtur for
                // "Forfaller snart".
                Medisiner = d.Medisiner
                    .AsQueryable()
                    .Where(aktivMedisin)
                    .OrderBy(m => m.Navn)
                    .Select(m => new AktivMedisin(
                        d.Id,
                        d.Navn,
                        m.Id,
                        m.Navn,
                        m.Dose,
                        m.IntervallTimer,
                        m.StartDato,
                        m.Doser
                            .OrderByDescending(x => x.GittTid)
                            .Select(x => (DateTimeOffset?)x.GittTid)
                            .FirstOrDefault()))
                    .ToList(),

                // Korrelert undersporring - siste foring i samme rundtur.
                SisteForing = d.Foringer
                    .OrderByDescending(f => f.Tidspunkt)
                    .Select(f => new
                    {
                        f.Tidspunkt,
                        Navn = f.GittAv == null ? null : f.GittAv.Visningsnavn
                    })
                    .FirstOrDefault(),

                // Enda to korrelerte undersporringer, ikke to nye rundturer.
                // Godbiter telles for seg: en ostebit er ikke middag.
                MaltiderIDag = d.Foringer.Count(f =>
                    f.Tidspunkt >= dagStart && f.Type == Foringstype.Maltid),

                GodbiterIDag = d.Foringer.Count(f =>
                    f.Tidspunkt >= dagStart && f.Type == Foringstype.Godbit)
            })
            .ToListAsync(ct);

        // Torrfortabellene til de aktive blandingsplanene. Egen rundtur, og
        // kun nar noen faktisk har en - en husstand uten forovergang skal
        // ikke betale for en tabell som ikke finnes.
        var trinn = raa.Any(d => d.Forplan?.Metode == Formetode.Tabell)
            ? (await _db.Forplantrinn
                .Where(t => t.Forplan.Aktiv)
                .OrderBy(t => t.AlderMnd)
                .Select(t => new { t.ForplanId, t.AlderMnd, t.GramPerDag })
                .ToListAsync(ct))
                .GroupBy(t => t.ForplanId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<Alderstrinn>)g
                        .Select(t => new Alderstrinn(t.AlderMnd, t.GramPerDag))
                        .ToList())
            : new Dictionary<int, IReadOnlyList<Alderstrinn>>();

        var dyr = raa.Select(d =>
        {
            // Bryteren styrer visning: er forplan slatt av for dyret, skal
            // den ikke dukke opp pa kortet heller.
            var regel = d.Forplan is null ? null : new Forplanregel(
                d.Forplan.Metode,
                d.Forplan.ProsentTidels,
                d.Forplan.GramPerDag,
                d.Forplan.AntallMaltider,
                d.Forplan.VektdelAndelProsent,
                trinn.GetValueOrDefault(d.Forplan.Id, []));

            var mengde = d.ForplanAktiv && regel is not null
                ? Forberegning.Beregn(regel, new Beregningsgrunnlag(
                    d.SisteVekt?.VektGram,
                    d.SisteVekt?.Dato,
                    d.Fodselsdato,
                    idag))
                : null;

            return new DyrKort(
                d.Id,
                d.Navn,
                d.Art,
                d.ProfilbildeId,
                d.Fodselsdato,
                d.ForingsloggAktiv,
                d.SisteVekt?.VektGram,
                d.SisteVekt?.Dato,
                Forplantekst(mengde),
                d.Neste is null ? null : TypeTekst(d.Neste.Type, d.Neste.Preparat),
                d.Neste?.Dato,
                d.Medisiner.Select(m => m.Navn).ToList(),
                // Kortet vises kun nar bryteren er pa for dyret. Er den av,
                // skal "sist matet" ikke dukke opp i det hele tatt.
                d.ForingsloggAktiv && d.SisteForing is not null
                    ? new SistMatet(d.SisteForing.Tidspunkt, d.SisteForing.Navn)
                    : null,
                // Uten vektgrunnlag finnes det ikke noe tall a vise, og da
                // skal knappen heller ikke tilby en porsjon.
                mengde is { HarPlan: true, ManglerGrunnlag: false }
                    ? mengde.PorsjonGram
                    : null,
                mengde?.AntallMaltider ?? 0,
                // Telles bare nar loggen er pa. Er den av, finnes det ingen
                // maltider a telle, og "0 av 2" ville vaert et falskt
                // etterslep for et dyr som ikke skal foringsloggfores.
                d.ForingsloggAktiv ? d.MaltiderIDag : 0,
                d.Forplan?.Fornavn,
                // Blandingsforholdet er hele poenget med overgangsplanen.
                // Uten det viser kortet en sum brukeren ma dele opp selv.
                mengde is { HarPlan: true, ManglerGrunnlag: false,
                            Fordeling.Blander: true }
                    ? new Porsjonsdeling(
                        mengde.VektdelPorsjonGram, d.Forplan!.Fornavn,
                        mengde.AldersdelPorsjonGram, d.Forplan.FornavnAlder)
                    : null,
                d.ForingsloggAktiv ? d.GodbiterIDag : 0);
        }).ToList();

        // Sporring 2. Behandlinger som forfaller innen vinduet, og som ikke
        // er fulgt opp av en nyere behandling av samme slag. Se ADR 0016.
        //
        // Medisiner kommer ikke herfra. De gjentas per time, ikke per dato,
        // og hentes allerede med dyrene i sporring 1.
        var forfallerRaa = await _db.Behandling
            .Where(Behandlingsfilter.ApenPaminnelse)
            .Where(b => b.NesteDato <= grense)
            .OrderBy(b => b.NesteDato)
            .Select(b => new
            {
                b.Id,
                b.DyrId,
                GittDato = b.Dato,
                DyreNavn = b.Dyr.Navn,
                b.Type,
                b.Preparat,
                Dato = b.NesteDato!.Value
            })
            .ToListAsync(ct);

        // Sporring 3. Forsikringer som skal fornyes innen vinduet.
        var forsikringer = await _db.Forsikring
            .Where(f => f.FornyesDato != null && f.FornyesDato <= grense)
            .OrderBy(f => f.FornyesDato)
            .Select(f => new
            {
                f.Id,
                f.DyrId,
                DyreNavn = f.Dyr.Navn,
                f.Selskap,
                Dato = f.FornyesDato!.Value
            })
            .ToListAsync(ct);

        // Sporring 4. Veterinaertimer: bade kommende timer og avtalt
        // oppfolging, hentet i SAMME rundtur. To sporringer mot samme tabell
        // ville vaert en rundtur for mye for det som er ett sporsmal - hva
        // skjer hos veterinaeren de neste ukene.
        var vetbesok = await _db.Vetbesok
            .Where(v => (v.Dato >= idag && v.Dato <= grense)
                     || (v.NesteKontrollDato != null
                         && v.NesteKontrollDato >= idag
                         && v.NesteKontrollDato <= grense))
            .Select(v => new
            {
                v.Id,
                v.DyrId,
                DyreNavn = v.Dyr.Navn,
                v.Dato,
                v.Klokkeslett,
                v.Arsak,
                v.NesteKontrollDato,
                Sted = v.Veterinar == null ? v.Klinikk : v.Veterinar.Navn
            })
            .ToListAsync(ct);

        // Teksten bygges etter materialisering. En switch over enum lar seg
        // ikke oversette til SQL, og det er unodvendig a prove.
        var forfaller = forfallerRaa
            .Select(b => new Paminnelse(
                b.DyrId,
                b.DyreNavn,
                Kilde.Behandling,
                b.Id,
                TypeTekst(b.Type, b.Preparat),
                b.Dato,
                // Gitt i dag betyr ingen "gitt"-knapp. Paminnelsen star, men
                // den er ikke noe a krysse av for i morgen.
                KanFolgesOpp: Behandlingsintervall.KanGisIgjen(b.GittDato, idag)))
            .Concat(forsikringer.Select(f => new Paminnelse(
                f.DyrId,
                f.DyreNavn,
                Kilde.Forsikring,
                f.Id,
                $"Fornyelse {f.Selskap}",
                f.Dato)))
            .Concat(Doser(raa.SelectMany(d => d.Medisiner), naa))
            // Selve timen. Klokkeslettet tas med nar det finnes - "torsdag"
            // er ubrukelig hvis timen er 08:15 og du ma ta fri.
            .Concat(vetbesok
                .Where(v => v.Dato >= idag && v.Dato <= grense)
                .Select(v => new Paminnelse(
                    v.DyrId,
                    v.DyreNavn,
                    Kilde.Vetbesok,
                    v.Id,
                    v.Klokkeslett is { } kl
                        ? $"Vet. kl. {kl:HH}:{kl:mm} – {v.Arsak}"
                        : $"Veterinær – {v.Arsak}",
                    v.Dato)))
            // Avtalt oppfolging fra et gjennomfort besok. En egen rad, fordi
            // den forfaller pa en annen dato enn timen den kom fra.
            .Concat(vetbesok
                .Where(v => v.NesteKontrollDato is { } d
                            && d >= idag && d <= grense)
                .Select(v => new Paminnelse(
                    v.DyrId,
                    v.DyreNavn,
                    Kilde.Vetkontroll,
                    v.Id,
                    $"Kontroll{(v.Sted is null ? "" : $" hos {v.Sted}")}",
                    v.NesteKontrollDato!.Value)))
            // Sortert stigende gir forfalte forst - de har eldst dato. Doser
            // som er over tiden i dag, legges foran resten av dagen.
            .OrderBy(p => p.Dato)
            .ThenByDescending(p => p.Overtid)
            .ToList();

        // Sporring 5. De fem oeverste aktive punktene pa handlelisten.
        var handleliste = await _handleliste.HentAktive(AntallPaHandleliste, ct);

        // Sporring 6. Husstandsbryteren for godbitloggen.
        //
        // Forslagene til dialogen hentes IKKE her. De ville vaert en sjette
        // rundtur pa hver eneste sidelast, for en liste de fleste aldri ser -
        // dialogen henter sitt eget innhold nar den apnes.
        var godbit = await _db.HusstandInnstilling
            .Select(i => (bool?)i.GodbitloggAktiv)
            .FirstOrDefaultAsync(ct) ?? true;

        // Sporring 7. Forsikringene som gjelder na.
        //
        // Kunne ikke slas sammen med sporring 3. Den henter fornyelser
        // innenfor varselvinduet - altsa de som snart gar ut. Dette er det
        // motsatte utvalget: de som fortsatt lopet, inkludert alle uten
        // fornyelsesdato i det hele tatt.
        var gjeldendeForsikringer = await _db.Forsikring
            .Where(f => f.FornyesDato == null || f.FornyesDato >= idag)
            .OrderBy(f => f.Dyr.Navn)
            .ThenBy(f => f.Selskap)
            .Select(f => new ForsikringRad(
                f.Id, f.DyrId, f.Dyr.Navn, f.Selskap, f.PoliseNr,
                f.ArspremieKr, f.ForsikringsbelopKr, f.EgenandelFastKr,
                f.EgenandelVariabelTidels, f.FornyesDato))
            .ToListAsync(ct);

        // Sporring 8. Stedene som kan ringes.
        //
        // Kunne ikke slas sammen med sporring 4 heller. Den henter besok
        // innenfor varselvinduet; dette er alle steder husstanden har lagret,
        // ogsa de uten et eneste besok. Det er nettopp vakta man aldri har
        // brukt som skal sta der klokka to om natten.
        //
        // Filtreres pa telefon i SQL: et sted uten nummer har ingenting a
        // gjore i en liste hvis eneste formal er a ringes.
        var vetRaa = await _db.Veterinar
            .Where(v => v.Telefon != null)
            .OrderBy(v => v.Navn)
            .Select(v => new Veterinarrad(
                v.Id, v.Navn, v.Type, v.Telefon, v.Adresse, v.Nettside,
                v.Epost,
                new Apningstider(
                    v.ApentMandag, v.ApentTirsdag, v.ApentOnsdag,
                    v.ApentTorsdag, v.ApentFredag, v.ApentLordag,
                    v.ApentSondag),
                v.Notat,
                // Korrelert undersporring, ingen rundtur per rad.
                v.Besok.Count))
            .ToListAsync(ct);

        // Sorteres HER, ikke i SQL. Type har HasConversion, sa databasen
        // sorterer pa det lagrede tegnet - 'A', 'F', 'S', 'V' - og da havner
        // Annet oeverst og vakta nest sist. Samme felle som i
        // VeterinarService.Hent, og den er stille: listen kommer sortert,
        // bare feil sortert.
        var veterinarer = vetRaa
            .OrderBy(v => v.Type)
            .ThenBy(v => v.Navn)
            .ToList();

        // Atte sporringer, og tallet star stille uansett hvor mange dyr
        // husstanden har. Det er det taket i kapittel 16 handler om: en ny
        // kilde koster hoyst en fast rundtur, aldri en per rad. Kan en kilde
        // slas sammen med en eksisterende sporring, skal den det - slik
        // vetbesok henter bade timer og oppfolging i samme kall.
        return new Dashbord(
            dyr, forfaller, handleliste, godbit,
            gjeldendeForsikringer, veterinarer);
    }

    /// <summary>
    /// En aktiv medisin slik sporring 1 henter den, med det som trengs for a
    /// avgjore om en dose forfaller i dag.
    /// </summary>
    private sealed record AktivMedisin(
        int DyrId,
        string DyreNavn,
        int Id,
        string Navn,
        string Dose,
        int IntervallTimer,
        DateOnly StartDato,
        DateTimeOffset? SisteDose);

    /// <summary>
    /// Medisindosene som forfaller i dag, en rad per medisin. Medisinene er
    /// allerede aktive - Medisinfilter i sporringen - og regelen for hva som
    /// forfaller ligger i <see cref="Dosevarsel"/>.
    /// </summary>
    private static IEnumerable<Paminnelse> Doser(
        IEnumerable<AktivMedisin> medisiner, DateTimeOffset naa)
    {
        foreach (var m in medisiner)
        {
            if (Dosevarsel.NesteDose(m.IntervallTimer, m.StartDato, m.SisteDose, naa)
                is not { } neste)
            {
                continue;
            }

            var nar = m.SisteDose is null
                ? "ingen doser gitt ennå"
                : neste <= naa
                    ? $"skulle vært gitt {Tidssone.NaerTid(neste, naa)}"
                    : $"neste dose kl. {Tidssone.Klokke(neste)}";

            yield return new Paminnelse(
                m.DyrId,
                m.DyreNavn,
                Kilde.Medisin,
                m.Id,
                $"{m.Navn} · {m.Dose} – {nar}",
                Tidssone.Idag(neste),
                Overtid: neste <= naa);
        }
    }

    private static string TypeTekst(BehandlingType type, string? preparat)
        => Behandlingsformat.MedPreparat(type, preparat);

    private static string? Forplantekst(ForplanResultat? mengde) => mengde switch
    {
        null => null,
        { ManglerVekt: true } => "Mangler vekt",
        { ManglerFodselsdato: true } => "Mangler fødselsdato",
        _ => $"{mengde.GramPerDag} g på {mengde.AntallMaltider} måltider"
    };
}
