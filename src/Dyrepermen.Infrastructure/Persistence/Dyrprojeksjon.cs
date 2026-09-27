using System.Linq.Expressions;
using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Entities;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Infrastructure.Persistence;

/// <summary>
/// Projeksjonen fra dyr til <see cref="DyrDetaljer"/>, delt av dyrets side
/// og utskriften. To kopier med fjorten felter spriker ved forste endring -
/// og profilbildet kom nettopp inn som en undersporring begge matte ha.
/// </summary>
public static class Dyrprojeksjon
{
    public static readonly Expression<Func<Dyr, DyrDetaljer>> Detaljer =
        d => new DyrDetaljer(
            d.Id, d.Navn, d.Art, d.Kjonn, d.Rase, d.Fodselsdato,
            d.ChipNr, d.RegNrNkk, d.Kastrert,
            d.Farge, d.Kjennetegn,
            // Korrelert undersporring i samme rundtur. Den unike indeksen
            // sikrer at det finnes hoyst ett.
            d.Dokumenter
                .Where(x => x.Kategori == DokumentKategori.Profilbilde)
                .Select(x => (int?)x.Id)
                .FirstOrDefault(),
            d.ForingsloggAktiv, d.ForplanAktiv);
}
