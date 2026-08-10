# 0003 — `dist/` i .gitignore må scopes til `clients/`

**Status:** Vedtatt
**Dato:** 2026-08-10
**Gjelder:** plan kapittel 3.6

## Kontekst

Planens `.gitignore` har under overskriften «Node, for fremtidig klient i
`clients/`» et bart mønster:

```gitignore
dist/
```

Git tolker et mønster uten skråstrek foran som «matcher i alle mapper, på
alle nivåer». Regelen treffer derfor ikke bare en fremtidig node-klient, men
også de vendorede bibliotekene MVC-templaten legger i:

```
src/Dyrepermen.Web/wwwroot/lib/bootstrap/dist/
src/Dyrepermen.Web/wwwroot/lib/jquery/dist/
src/Dyrepermen.Web/wwwroot/lib/jquery-validation/dist/
```

Verifisert med `git check-ignore -v` før commit: 60 filer, deriblant hele
Bootstrap 5.3.3 og jQuery-valideringen, ville blitt utelatt fra repoet.

Feilen er ubehagelig fordi den ikke gir noen feilmelding. Bygget blir grønt,
testene blir grønne, og appen kjører fint lokalt — der filene ligger på disk.
Først i Docker eller på Render, som bygger fra det Git faktisk inneholder,
kommer appen opp uten CSS og uten JavaScript. Da er `offcanvas`-menyen i
kapittel 10.2 død, og symptomet peker ingen steder mot `.gitignore`.

## Beslutning

Node-mønstrene scopes til mappa de er ment for:

```gitignore
node_modules/
clients/**/dist/
clients/**/.vite/
```

`node_modules/` beholdes globalt — den mappa skal aldri versjonshåndteres
uansett hvor den dukker opp.

Begrunnelsen står som kommentar i `.gitignore` selv, ikke bare her. Neste
person som rydder i fila skal se hvorfor mønsteret er scopet.

## Konsekvens

- Bootstrap og jQuery ligger i repoet, og Docker-bygget får dem med.
- Bygges React-klienten senere i `clients/`, dekker mønstrene den fortsatt.
- Legges et byggesteg som skriver til en annen `dist/`-mappe, må mønsteret
  utvides eksplisitt. Det er tilsiktet — regelen skal være synlig, ikke bred.
