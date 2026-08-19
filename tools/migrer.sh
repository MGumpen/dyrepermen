#!/usr/bin/env bash
#
# Genererer et idempotent migrasjonsskript og kjorer det.
#
# TRENGS IKKE I DAGLIG BRUK. Applikasjonen migrerer selv ved oppstart, rett
# for app.Run() i Program.cs - se ADR 0010. Bade lokalt, i containeren og pa
# Render er det nok a starte appen.
#
# Skriptet finnes for de tilfellene der oppstartsmigrering ikke holder:
#
#   - Appen skaleres ut. Oppstartsmigrering forutsetter EN instans; kjorer to
#     instanser oppstart samtidig, migrerer de samtidig. Da ma kallet ut av
#     Program.cs og skjemaet legges inn her forst, som eget steg.
#   - Du vil se hvilken SQL en migrasjon faktisk gir, for den kjores.
#   - Skjemaet skal legges inn i en database appen ikke har tilgang til.
#
# Skriptet er idempotent: det leser __EFMigrationsHistory og kjorer bare det
# som mangler, akkurat som MigrateAsync gjor ved oppstart.
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
