using Dyrepermen.Application.Dtos;
using Dyrepermen.Domain.Enums;

namespace Dyrepermen.Application.Interfaces;

public interface IKontaktService
{
    /// <summary>
    /// Sender henvendelsen pa e-post til den som lager appen.
    ///
    /// Avsenderen hentes fra innloggingen, ikke fra skjemaet. Kom navn og
    /// adresse fra klienten, kunne hvem som helst sendt i en annens navn.
    /// </summary>
    Task<Kontaktresultat> Send(Kontakttype type, string melding, CancellationToken ct);
}
