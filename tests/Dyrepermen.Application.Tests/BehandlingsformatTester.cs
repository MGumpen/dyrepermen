using Dyrepermen.Application.Extensions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Tests;

/// <summary>
/// Teksten en behandling vises med i historikken, pa dashbordet og i
/// dialogen for "gitt i dag".
/// </summary>
public sealed class BehandlingsformatTester
{
    [Theory]
    [InlineData(null, null, "Ormekur")]
    [InlineData("Milbemax", null, "Ormekur – Milbemax")]
    [InlineData("Milbemax", "12,5 mg/125 mg", "Ormekur – Milbemax 12,5 mg/125 mg")]
    [InlineData(null, "2,5 ml", "Ormekur – 2,5 ml")]
    [InlineData("Milbemax", " ", "Ormekur – Milbemax")]
    public void Preparat_og_styrke(string? preparat, string? styrke, string tekst)
    {
        Assert.Equal(tekst,
            Behandlingsformat.MedPreparat(BehandlingType.Ormekur, preparat, styrke));
    }
}
