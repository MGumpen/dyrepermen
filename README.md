# Dyrepermen

Webapplikasjon for oppfølging av husstandens kjæledyr.

ASP.NET Core MVC (.NET 9) · Entity Framework Core · PostgreSQL · Monorepo

## Dokumentasjon

| Fil | Innhold |
|---|---|
| `CLAUDE.md` | Arbeidsinstruks: kodekonvensjoner, kommandoer, fallgruver |
| `docs/plan.md` | Full teknisk spesifikasjon |
| `docs/plan.pdf` | Samme dokument for lesing |
| `docs/beslutninger/` | ADR-er — én fil per beslutning tatt underveis |

## Kom i gang

```bash
docker compose -f infra/compose.yaml up -d db

dotnet ef database update \
  --project src/Dyrepermen.Infrastructure \
  --startup-project src/Dyrepermen.Web

dotnet run --project src/Dyrepermen.Web
```

## Omfang

MVP er fase 1, 1b og 2 i `docs/plan.md` kapittel 16. Akseptansekriteriene i
samme kapittel er definisjonen av ferdig.
