namespace Dyrepermen.Application.Dtos;

/// <summary>
/// Resultatet av et oppslag i torrfortabellen.
///
/// <see cref="UtenforTabellen"/> er sant nar dyret er yngre enn forste rad
/// eller eldre enn siste. Da holdes mengden pa naermeste rad, og det skal
/// sies - et flatt tall som ser ut som en beregning er verre enn et tall med
/// en merknad.
/// </summary>
public sealed record Taboppslag(int Gram, bool UtenforTabellen);
