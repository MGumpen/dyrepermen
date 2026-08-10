#!/usr/bin/env bash
#
# Genererer et idempotent migrasjonsskript og kjorer det.
#
# Migrasjoner kjores ALDRI ved oppstart av applikasjonen. Et idempotent
# skript gir kontroll over nar skjemaendringer skjer, og unngar at to
# instanser migrerer samtidig. Se plan kapittel 14.3.
#
# Bruk:
#   tools/migrer.sh                 skriv skriptet, ikke kjor det
#   tools/migrer.sh --kjor          skriv og kjor mot $DATABASE_URL
#
# DATABASE_URL ma vaere i postgres://-format for psql. Applikasjonen bruker
# nokkel/verdi-format for Npgsql - de to er ikke utbyttbare.

set -euo pipefail

ROT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROT"

UT="migrations.sql"

echo "Genererer idempotent migrasjonsskript -> $UT"
dotnet ef migrations script --idempotent \
  --project src/Dyrepermen.Infrastructure \
  --startup-project src/Dyrepermen.Web \
  --output "$UT"

if [[ "${1:-}" != "--kjor" ]]; then
    echo "Skriptet er skrevet. Kjor med --kjor for a legge det inn i databasen."
    exit 0
fi

if [[ -z "${DATABASE_URL:-}" ]]; then
    echo "FEIL: DATABASE_URL er ikke satt." >&2
    exit 1
fi

# ON_ERROR_STOP=1 er kritisk. Uten den fortsetter psql etter en feilet
# setning og rapporterer suksess, mens skjemaet er halvferdig.
echo "Kjorer migrasjoner"
psql "$DATABASE_URL" -v ON_ERROR_STOP=1 -f "$UT"

echo "Ferdig."
