using Dyrepermen.Application.Interfaces;
using Dyrepermen.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dyrepermen.Infrastructure;

/// <summary>
/// Samler DI-registreringen av infrastrukturtjenester ett sted, slik at
/// Program.cs slipper a kjenne implementasjonstypene. Se ADR 0007.
/// </summary>
public static class TjenesteRegistrering
{
    public static IServiceCollection LeggTilInfrastruktur(
        this IServiceCollection tjenester)
    {
        tjenester.AddScoped<IHusstandService, HusstandService>();
        return tjenester;
    }
}
