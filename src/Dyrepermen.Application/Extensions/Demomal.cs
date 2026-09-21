using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Extensions;

/// <summary>
/// Innholdet i en fersk demohusstand. Se ADR 0015.
///
/// Faste data, ingen tilfeldighet. Kun datoene folger <c>naa</c>, slik at det
/// aldri ser ut som det er lenge siden noe ble registrert. Ren funksjon uten
/// databasetilgang: DemoService legger resultatet inn som det er.
///
/// Ingenting her skal kunne kjennes igjen. Klinikkene og forsikringsselskapet
/// er oppdiktet, og chipnummer er utelatt - et gyldig nummer kan tilhore noen.
/// Telefonnumrene er valgt av Marius. Uten dem viser ikke Oversikt
/// veterinarene, fordi kortet der bare lister steder som kan ringes.
/// </summary>
public static class Demomal
{
    private static readonly TimeOnly Frokost = new(7, 30);
    private static readonly TimeOnly Middag = new(16, 30);
    private static readonly TimeOnly Morgendose = new(8, 0);

    public static Demodata Bygg(Bruker bruker, DateTimeOffset naa)
    {
        var idag = Tidssone.Idag(naa);

        var husstand = new Husstand
        {
            Navn = "Demohusstanden",
            OpprettetDato = idag,
            // Alle funksjonsbrytere pa, sa demoen viser alt.
            Innstilling = new HusstandInnstilling
            {
                ForingsloggStandard = true,
                ForplanStandard = true,
                VarslerAktiv = true,
                GodbitloggAktiv = true
            }
        };

        husstand.Medlemskap.Add(new Husstandsmedlemskap
        {
            Bruker = bruker,
            Rolle = Husstandsrolle.Beboer,
            OpprettetDato = idag
        });

        var klinikk = new Veterinar
        {
            Husstand = husstand,
            Navn = "Demoklinikken",
            Type = Veterinartype.Fast,
            Telefon = "12345678",
            ApentMandag = "08–16",
            ApentTirsdag = "08–16",
            ApentOnsdag = "08–16",
            ApentTorsdag = "08–16",
            ApentFredag = "08–16",
            ApentLordag = "10–14",
            OpprettetDato = idag
        };

        var vakt = new Veterinar
        {
            Husstand = husstand,
            Navn = "Demo døgnvakt",
            Type = Veterinartype.Vakt,
            Telefon = "11223344",
            ApentMandag = "Døgnåpent",
            ApentTirsdag = "Døgnåpent",
            ApentOnsdag = "Døgnåpent",
            ApentTorsdag = "Døgnåpent",
            ApentFredag = "Døgnåpent",
            ApentLordag = "Døgnåpent",
            ApentSondag = "Døgnåpent",
            OpprettetDato = idag
        };

        // --- Trixie: voksen hund med forsikring, kur og forfalt vaksine -----

        var forsikring = new Forsikring
        {
            Selskap = "Demoforsikring",
            PoliseNr = "DEMO-1001",
            ArspremieKr = 4_800,
            ForsikringsbelopKr = 30_000,
            EgenandelFastKr = 1_500,
            EgenandelVariabelTidels = 250,
            FornyesDato = idag.AddMonths(5)
        };

        var kur = new Medisin
        {
            Navn = "Smertestillende",
            Dose = "1 tablett",
            IntervallTimer = 24,
            StartDato = idag.AddDays(-6),
            SluttDato = idag.AddDays(4)
        };

        // En dose hver morgen til og med i gar. Neste dose er da i dag.
        for (var dag = kur.StartDato; dag < idag; dag = dag.AddDays(1))
        {
            kur.Doser.Add(new Dose { GittTid = Kl(dag, Morgendose), GittAv = bruker });
        }

        var trixie = new Dyr
        {
            Navn = "Trixie",
            Art = Art.Hund,
            Rase = "Labrador retriever",
            Kjonn = Kjonn.Tispe,
            Fodselsdato = idag.AddYears(-4).AddMonths(-3),
            ForingsloggAktiv = true,
            Forplaner =
            {
                new Forplan
                {
                    Metode = Formetode.Gram,
                    GramPerDag = 320,
                    AntallMaltider = 2,
                    Fornavn = "Tørrfôr voksen",
                    OpprettetDato = idag.AddMonths(-6)
                }
            },
            Behandlinger =
            {
                // Forfalt for fem dager siden - det Oversikt skal vise forst.
                new Behandling
                {
                    Type = BehandlingType.Vaksine,
                    Preparat = "Kombinasjonsvaksine",
                    Dato = idag.AddYears(-1).AddDays(-5),
                    NesteDato = idag.AddDays(-5)
                },
                // Historikk uten neste dato. Med en, ville den ogsa statt som
                // forfalt.
                new Behandling
                {
                    Type = BehandlingType.Vaksine,
                    Preparat = "Kombinasjonsvaksine",
                    Dato = idag.AddYears(-2).AddDays(-5)
                },
                new Behandling
                {
                    Type = BehandlingType.Ormekur,
                    Dato = idag.AddDays(-80),
                    NesteDato = idag.AddDays(10)
                }
            },
            Medisiner = { kur },
            Forsikringer = { forsikring },
            Vetbesok =
            {
                new Vetbesok
                {
                    Veterinar = klinikk,
                    Dato = idag.AddDays(6),
                    Klokkeslett = new TimeOnly(10, 15),
                    Arsak = "Årlig kontroll"
                },
                new Vetbesok
                {
                    Veterinar = vakt,
                    Dato = idag.AddDays(-95),
                    Arsak = "Halter på venstre framben",
                    Diagnose = "Forstuing",
                    KostnadKr = 2_400,
                    ForsikringKrevd = true,
                    RefundertKr = 2_400 - forsikring.Egenandel(2_400)
                }
            }
        };

        // --- Rose: valp som blander rafor etter vekt og torrfor etter alder --

        var rose = new Dyr
        {
            Navn = "Rose",
            Art = Art.Hund,
            Rase = "Border collie",
            Kjonn = Kjonn.Tispe,
            // Et fast antall dager, ikke maneder. Da er alderen i hele uker -
            // det fortabellen slar opp pa - den samme uansett startdag.
            Fodselsdato = idag.AddDays(-157),
            ForingsloggAktiv = true,
            Forplaner =
            {
                new Forplan
                {
                    // Halvparten av dagsmengden er rafor etter vekt, den andre
                    // halvparten torrfor etter tabellen. Kortet viser da begge
                    // delene hver for seg.
                    Metode = Formetode.Tabell,
                    VektdelAndelProsent = 50,
                    ProsentTidels = 50,
                    AntallMaltider = 2,
                    Fornavn = "Råfôr valp",
                    FornavnAlder = "Tørrfôr valp",
                    OpprettetDato = idag.AddDays(-70),
                    // Rose er mellom 4 og 6 maneder, sa mengden trappes
                    // mellom to rader.
                    Tabelltrinn =
                    {
                        new Forplantrinn { AlderMnd = 2, GramPerDag = 220 },
                        new Forplantrinn { AlderMnd = 4, GramPerDag = 270 },
                        new Forplantrinn { AlderMnd = 6, GramPerDag = 300 },
                        new Forplantrinn { AlderMnd = 12, GramPerDag = 280 }
                    }
                }
            },
            Behandlinger =
            {
                new Behandling
                {
                    Type = BehandlingType.Vaksine,
                    Preparat = "Valpevaksine",
                    Dato = idag.AddDays(-30),
                    NesteDato = idag.AddDays(60)
                }
            }
        };

        // --- Milo: katt i prosent av vekt -----------------------------------

        var milo = new Dyr
        {
            Navn = "Milo",
            Art = Art.Katt,
            Rase = "Norsk skogkatt",
            Kjonn = Kjonn.Hann,
            Fodselsdato = idag.AddYears(-2).AddMonths(-7),
            Kastrert = true,
            // Uten foringslogg. Oversikt viser porsjonen, men ingen knapper for
            // a gi mat eller godbit - demoen viser at bryteren finnes.
            Forplaner =
            {
                new Forplan
                {
                    Metode = Formetode.Prosent,
                    ProsentTidels = 20,
                    AntallMaltider = 2,
                    Fornavn = "Tørrfôr katt",
                    OpprettetDato = idag.AddYears(-1)
                }
            },
            Behandlinger =
            {
                new Behandling
                {
                    Type = BehandlingType.Vaksine,
                    Preparat = "Kattevaksine",
                    Dato = idag.AddDays(-200),
                    NesteDato = idag.AddDays(165)
                },
                new Behandling { Type = BehandlingType.Kloklipp, Dato = idag.AddDays(-20) }
            },
            Vetbesok =
            {
                new Vetbesok
                {
                    Veterinar = klinikk,
                    Dato = idag.AddDays(-200),
                    Arsak = "Vaksinering",
                    KostnadKr = 850
                }
            }
        };

        // --- Vekt og foring ---------------------------------------------------

        var sisteVeiing = idag.AddDays(-2);
        Veiinger(trixie, bruker, sisteVeiing, dagerMellom: 30,
            24_600, 24_800, 25_100, 25_000, 24_700, 24_900,
            25_300, 25_400, 25_200, 25_000, 24_800, 24_900);
        Veiinger(rose, bruker, sisteVeiing, dagerMellom: 7,
            4_200, 4_900, 5_600, 6_300, 6_900, 7_500, 8_100,
            8_600, 9_100, 9_600, 10_000, 10_400, 10_800);
        Veiinger(milo, bruker, sisteVeiing, dagerMellom: 30,
            4_000, 4_100, 4_100, 4_200, 4_100, 4_200);

        foreach (var dyr in new[] { trixie, rose, milo })
        {
            // Uten foringslogg finnes det ingen maltider a logge.
            if (dyr.ForingsloggAktiv)
            {
                Maltider(dyr, bruker, idag, naa);
            }

            husstand.Dyr.Add(dyr);
        }

        trixie.Foringer.Add(new Foring
        {
            Tidspunkt = Kl(idag.AddDays(-1), new TimeOnly(20, 0)),
            Type = Foringstype.Godbit,
            Fornavn = "Ostebit",
            GittAv = bruker
        });

        // --- Handleliste og informasjon --------------------------------------

        husstand.Handleliste.Add(new Handleliste
        {
            Tekst = "Tørrfôr voksen, 12 kg",
            Dyr = trixie,
            OpprettetAv = bruker,
            OpprettetDato = idag.AddDays(-3)
        });
        husstand.Handleliste.Add(new Handleliste
        {
            Tekst = "Kattesand",
            Dyr = milo,
            OpprettetAv = bruker,
            OpprettetDato = idag.AddDays(-1)
        });
        husstand.Handleliste.Add(new Handleliste
        {
            Tekst = "Tyggeben",
            Dyr = rose,
            Status = HandlelisteStatus.Kjopt,
            OpprettetAv = bruker,
            OpprettetDato = idag.AddDays(-4)
        });

        var informasjon = new List<Informasjon>
        {
            new()
            {
                Husstand = husstand,
                Tittel = "Turer",
                Tekst = "Trixie går tur morgen og kveld. Rose blir med på korte "
                        + "turer, høyst 20 minutter mens hun vokser.",
                OpprettetAv = bruker,
                OpprettetDato = idag.AddDays(-30)
            },
            new()
            {
                Husstand = husstand,
                Dyr = milo,
                Tittel = "Allergier",
                Tekst = "Milo tåler ikke kylling. Sjekk innholdet før du gir "
                        + "ham godbiter.",
                OpprettetAv = bruker,
                OpprettetDato = idag.AddDays(-30)
            }
        };

        return new Demodata(husstand, [klinikk, vakt], informasjon);
    }

    /// <summary>
    /// Veiingene med fast mellomrom, eldste forst, slik at den siste faller
    /// pa <paramref name="siste"/>.
    /// </summary>
    private static void Veiinger(
        Dyr dyr, Bruker bruker, DateOnly siste, int dagerMellom, params int[] gram)
    {
        for (var i = 0; i < gram.Length; i++)
        {
            dyr.Vekter.Add(new Vekt
            {
                VektGram = gram[i],
                Dato = siste.AddDays(-dagerMellom * (gram.Length - 1 - i)),
                RegistrertAv = bruker
            });
        }
    }

    /// <summary>
    /// Alle maltider i gar, og bare frokosten i dag - sa Oversikt viser
    /// «Gi mat» for middagen. Uten fornavn, akkurat som «Gi mat» lagrer dem.
    /// </summary>
    private static void Maltider(Dyr dyr, Bruker bruker, DateOnly idag, DateTimeOffset naa)
    {
        var porsjon = Porsjon(dyr, idag);
        var igar = idag.AddDays(-1);

        foreach (var tid in new[] { Kl(igar, Frokost), Kl(igar, Middag), TidligereIdag(idag, Frokost, naa) })
        {
            dyr.Foringer.Add(new Foring
            {
                Tidspunkt = tid,
                Type = Foringstype.Maltid,
                MengdeGram = porsjon,
                GittAv = bruker
            });
        }
    }

    /// <summary>
    /// Samme beregning som «Gi mat» bruker, sa loggen stemmer med planen.
    /// </summary>
    private static int Porsjon(Dyr dyr, DateOnly idag)
    {
        var plan = dyr.Forplaner.Single();
        var siste = dyr.Vekter.MaxBy(v => v.Dato)!;

        var regel = new Forplanregel(
            plan.Metode,
            plan.ProsentTidels,
            plan.GramPerDag,
            plan.AntallMaltider,
            plan.VektdelAndelProsent,
            plan.Tabelltrinn.Select(t => new Alderstrinn(t.AlderMnd, t.GramPerDag)).ToList());

        return Forberegning.Beregn(
                regel,
                new Beregningsgrunnlag(siste.VektGram, siste.Dato, dyr.Fodselsdato, idag))
            .PorsjonGram;
    }

    /// <summary>
    /// «I dag»-regelen i ADR 0015: det som skal ha skjedd i dag, ligger aldri
    /// i framtiden. Er klokkeslettet ikke passert, legges hendelsen rett for
    /// <paramref name="naa"/> - og rett over midnatt blir det da i gar.
    /// </summary>
    private static DateTimeOffset TidligereIdag(DateOnly idag, TimeOnly klokke, DateTimeOffset naa)
    {
        var tid = Kl(idag, klokke);
        return tid < naa ? tid : naa.AddMinutes(-1).ToUniversalTime();
    }

    /// <summary>
    /// Norsk klokkeslett som UTC-tidspunkt. Forskyvningen hentes for akkurat
    /// det tidspunktet, sa sommer- og vintertid blir riktig. UTC fordi Npgsql
    /// bare tar imot forskyvning 0 i timestamptz.
    /// </summary>
    private static DateTimeOffset Kl(DateOnly dag, TimeOnly klokke)
    {
        var lokal = dag.ToDateTime(klokke);
        return new DateTimeOffset(lokal, Tidssone.Forskyvning(lokal)).ToUniversalTime();
    }
}
