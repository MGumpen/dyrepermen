using System.Text.Json;
using System.Text.Json.Serialization;
using Dyrepermen.Application.Dtos;
using Dyrepermen.Application.Extensions;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Demoen skal se fersk ut uansett nar den startes. Se ADR 0015.
///
/// Tidspunktene er valgt der feil viser seg: rett over midnatt, for
/// frokosten, siste minutt for midnatt, dagene sommertiden starter og
/// slutter, og en skuddag.
/// </summary>
public sealed class DemomalTester
{
    // Syklusene - vekten peker pa brukeren, veterinaren pa husstanden - kuttes,
    // ellers gar serialiseringen i ring.
    private static readonly JsonSerializerOptions Json =
        new() { ReferenceHandler = ReferenceHandler.IgnoreCycles };

    public static TheoryData<DateTimeOffset> Tidspunkter => new()
    {
        Norsk(2026, 9, 15, 0, 30),
        Norsk(2026, 9, 15, 6, 0),
        Norsk(2026, 9, 15, 23, 59),
        Norsk(2026, 3, 29, 1, 30),   // natten sommertiden starter
        Norsk(2026, 3, 29, 12, 0),
        Norsk(2026, 10, 25, 0, 30),  // natten sommertiden slutter
        Norsk(2026, 10, 25, 12, 0),
        Norsk(2028, 2, 29, 12, 0)    // skuddag - datoen finnes ikke et ar tilbake
    };

    [Fact]
    public void Samme_tidspunkt_gir_identisk_mal()
    {
        var bruker = new Bruker { Visningsnavn = "Demobruker" };
        var naa = Norsk(2026, 9, 15, 12, 0);

        Assert.Equal(
            JsonSerializer.Serialize(Demomal.Bygg(bruker, naa), Json),
            JsonSerializer.Serialize(Demomal.Bygg(bruker, naa), Json));
    }

    [Theory]
    [MemberData(nameof(Tidspunkter))]
    public void Ingenting_ligger_i_framtiden(DateTimeOffset naa)
    {
        var dyr = Bygg(naa).Husstand.Dyr;

        Assert.All(dyr.SelectMany(d => d.Foringer),
            f => Assert.True(f.Tidspunkt < naa));
        Assert.All(dyr.SelectMany(d => d.Medisiner).SelectMany(m => m.Doser),
            d => Assert.True(d.GittTid < naa));
        Assert.All(dyr.SelectMany(d => d.Vekter),
            v => Assert.True(v.Dato <= Tidssone.Idag(naa)));
    }

    [Theory]
    [MemberData(nameof(Tidspunkter))]
    public void Vaksinen_er_forfalt_og_timen_er_kommende(DateTimeOffset naa)
    {
        var idag = Tidssone.Idag(naa);
        var dyr = Bygg(naa).Husstand.Dyr;

        Assert.Contains(dyr.SelectMany(d => d.Behandlinger),
            b => b.Type == BehandlingType.Vaksine && b.NesteDato < idag);
        Assert.Contains(dyr.SelectMany(d => d.Vetbesok),
            v => v.Dato > idag);
    }

    [Fact]
    public void Midt_pa_dagen_er_frokosten_gitt_og_middagen_ikke()
    {
        // Ett av to maltider i dag - da viser Oversikt «Gi mat» for hvert dyr
        // med foringslogg.
        var naa = Norsk(2026, 9, 15, 12, 0);
        var dagStart = Tidssone.DagStart(naa);

        Assert.All(Bygg(naa).Husstand.Dyr.Where(d => d.ForingsloggAktiv), d => Assert.Equal(1,
            d.Foringer.Count(f => f.Type == Foringstype.Maltid && f.Tidspunkt >= dagStart)));
    }

    private static Demodata Bygg(DateTimeOffset naa)
        => Demomal.Bygg(new Bruker { Visningsnavn = "Demobruker" }, naa);

    private static DateTimeOffset Norsk(int ar, int mnd, int dag, int time, int min)
    {
        var lokal = new DateTime(ar, mnd, dag, time, min, 0);
        return new DateTimeOffset(lokal, Tidssone.Forskyvning(lokal));
    }
}
