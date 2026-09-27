using Dyrepermen.Domain.Abstractions;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Domain.Entities;

/// <summary>
/// En opplastet fil knyttet til et dyr, og eventuelt til et veterinaerbesok -
/// typisk kvitteringen. Selve filen ligger i <see cref="DokumentInnhold"/>, i
/// databasen og ikke pa disk: Render sin gratistjeneste mister alt som er
/// lagret lokalt hver gang den sovner. Se ADR 0018.
///
/// Filer serveres via en controller-handling som verifiserer
/// husstandstilhorighet, aldri direkte fra wwwroot. Se plan kapittel 15.
/// </summary>
public sealed class Dokument : IHusstandsbundet
{
    public int Id { get; set; }

    public int DyrId { get; set; }

    public Dyr Dyr { get; set; } = null!;

    /// <summary>
    /// Besoket filen er vedlegg til. Null for dokumenter som bare horer til
    /// dyret, som vaksineboka.
    /// </summary>
    public int? VetbesokId { get; set; }

    public Vetbesok? Vetbesok { get; set; }

    /// <summary>Brukerens eget filnavn, vises i grensesnittet.</summary>
    public string Originalnavn { get; set; } = null!;

    /// <summary>
    /// MIME-typen slik innholdet ble gjenkjent ved opplasting - ikke slik
    /// nettleseren oppga den.
    /// </summary>
    public string Innholdstype { get; set; } = null!;

    /// <summary>
    /// Storrelsen i byte. Egen kolonne sa husstandens forbruk kan summeres
    /// uten a lese selve filene.
    /// </summary>
    public int StorrelseByte { get; set; }

    public DokumentKategori Kategori { get; set; }

    public DateOnly OpplastetDato { get; set; }

    public DokumentInnhold Innhold { get; set; } = null!;
}
