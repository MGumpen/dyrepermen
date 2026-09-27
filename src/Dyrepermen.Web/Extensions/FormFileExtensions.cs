using Dyrepermen.Application.Dtos;

namespace Dyrepermen.Web.Extensions;

public static class FormFileExtensions
{
    /// <summary>
    /// Leser filen inn i minnet. Trygt fordi handlingene som tar imot filer,
    /// har RequestSizeLimit - en fil pa en gigabyte kommer aldri hit.
    /// </summary>
    public static async Task<NyttVedlegg> TilVedlegg(
        this IFormFile fil, CancellationToken ct)
    {
        using var minne = new MemoryStream((int)fil.Length);
        await fil.CopyToAsync(minne, ct);
        return new NyttVedlegg(fil.FileName, minne.ToArray());
    }
}
