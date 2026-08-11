# Dyrepermen

App for dyrehold. Under utvikling.

Denne branchen inneholder **kun landingssiden** — den som står på
produksjonsadressen mens appen bygges. Selve appen ligger på `dev`.

## Hvorfor landingssiden er en app og ikke en fil

`main` hadde tidligere bare en `index.html` i rota. Den kan ikke rulles ut:
Render bygger et Docker-image og kjører en webtjeneste, og en løs HTML-fil er
ingen tjeneste. Utrullingen hadde ingenting å starte.

Landingssiden er derfor et minimalt ASP.NET Core-prosjekt. Det er med vilje
et helt annet oppsett enn appens:

| | Appen (`dev`) | Landingssiden (`main`) |
|---|---|---|
| Database | Postgres, migrasjoner ved oppstart | ingen |
| Miljøvariabler | tilkoblingsstreng må settes | **ingen** |
| NuGet-pakker | EF Core, Npgsql, Identity | **ingen** |
| Feiler hvis databasen er nede | ja, med vilje | nei |

Poenget er at siden skal komme opp uansett. En side som sier «under
utvikling» skal ikke kunne feile fordi en database mangler.

## Kjør lokalt

```bash
dotnet run --project src/Dyrepermen.Landingsside
```

Eller som container, slik Render kjører den:

```bash
docker build -f infra/Dockerfile -t dyrepermen-landing .
docker run --rm -p 8080:8080 dyrepermen-landing
```

`GET /helse` svarer `200` uten å treffe noen database. Alle andre stier viser
landingssiden.

## Brancher

| Branch | Formål |
|---|---|
| `main` | Produksjon. Landingssiden, til appen er klar |
| `dev` | Integrasjon. Alt arbeid samles her |
| `feature/mvp` | Appen som bygges nå |
| `feature/landingsside` | Denne siden |

Arbeidsflyt: `feature/*` → `dev` → `main`.

## Når appen skal overta

Slå `dev` inn i `main`. Appen har sin egen `Program.cs`, sin egen
`infra/Dockerfile` og krever `ConnectionStrings__Postgres` satt i Render —
den tar imot både nøkkel/verdi og URI-en Neon oppgir.

Landingssiden trenger ingen opprydding før det: filene her erstattes av
appens egne i samme sammenslåing.
