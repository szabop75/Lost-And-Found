# Lost-And-Found – Fejlesztői és üzemeltetési útmutató (HU)

## Cél
Ez a dokumentum összefoglalja, hogyan konfiguráld és futtasd a projektet helyi fejlesztéshez és éles környezetben. A beállítások célja, hogy minden szenzitív adat környezeti változókból érkezzen, az `appsettings.json` ne tartalmazzon titkokat.

## Fájlok és szerepük
- `docker-compose.yml`: alap szolgáltatás-definíciók (API + frontend). Titkok itt sem szerepelnek, csak változónévre hivatkozunk.
- `docker-compose.override.yml`: helyi fejlesztési felülírás. A repo-ban NEM commitoljuk, benne a fejlesztési kényelmi beállítások (pl. dev adatbázis neve). Minta: `docker-compose.override.example.yml`.
- `.env`: helyi környezeti változók (NEM commitoljuk; benne a valódi értékek).
- `.env.example`: minta `.env`, szenzitív adatok nélkül (ezt commitoljuk).
- `backend/LostAndFound.Api/appsettings.json`: nem tartalmaz titkot; futáskor a környezet írja felül az értékeket.

## Környezeti változók (.env)
Helyi fejlesztéshez hozz létre egy `.env` fájlt a repo gyökerében az alábbi kulcsokkal:

```dotenv
# GHCR / proxy
GHCR_OWNER=github_user_vagy_org
PROXY_NETWORK=proxy_halozat_nev

# DB
EXTERNAL_DB_HOST=host.nev.vagy.ip
POSTGRES_PORT=5432
POSTGRES_DB=lostandfound
POSTGRES_USER=felhasznalo
POSTGRES_PASSWORD=jelszo

# ASP.NET
ASPNETCORE_ENVIRONMENT=Development

# JWT (dupla aláhúzás!)
JWT__SECRET=eros_titkos_kulcs
```

Megjegyzések:
- A `docker-compose.yml` ezekből állítja össze az `api` szolgáltatás `ConnectionStrings__DefaultConnection` értékét.
- A `.NET` automatikusan beolvassa a `ConnectionStrings__*` és `Jwt__*` változókat (a dupla aláhúzás szekciót jelent, pl. `Jwt:Secret`).

## Fejlesztési adatbázis (lostandfound_dev)
Fejlesztéskor külön adatbázist használunk: `lostandfound_dev`.
- Ehhez hozz létre a saját gépeden egy `docker-compose.override.yml` fájlt a mellékelt minta alapján:

```yaml
# docker-compose.override.yml (helyi gépen, git ignore alatt)
services:
  api:
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: "Host=${EXTERNAL_DB_HOST};Port=${POSTGRES_PORT};Database=lostandfound_dev;Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD};SSL Mode=Disable"
```

A repo-ban megtalálod a `docker-compose.override.example.yml` mintát. Ezt másold át `docker-compose.override.yml` néven a saját gépedre.

## Éles adatbázis
Élesben a `docker-compose.override.yml` nem kerül használatra, így a `docker-compose.yml`-ben összeálló kapcsolat a `.env`-ben megadott `POSTGRES_DB` értékét használja (pl. `lostandfound`). Éles környezetben mindig a szerveren lévő `.env` adja meg a titkokat.

## Futtatás
- Konfiguráció ellenőrzése: `docker compose config`
- Indítás: `docker compose up -d`
- Leállítás: `docker compose down`

A `docker compose` automatikusan beolvassa a gyökér `.env`-et, és ha létezik, az `docker-compose.override.yml`-t is.

## Git ignore és minta fájlok
- A `.gitignore` úgy van beállítva, hogy a `docker-compose.override.yml` és a `.env` ne kerüljön commitba.
- A `docker-compose.override.example.yml` és `.env.example` commitolva vannak, ezek mintaként szolgálnak.

## GitHub Actions és GHCR
- A `.github/workflows/docker-api.yml` és `docker-frontend.yml` a GHCR-be buildel és pushol image-eket a `main` branch-re történő push esetén.
- A build NEM igényel adatbázis/jogosultság/jwt titkokat; ezek futtatáskor kerülnek átadásra (pl. a szerveren futó `docker compose` az ottani `.env`-ből).
- A szerveren futtatáskor ugyanazokat a változókat kell megadnod `.env`-ben, mint fejlesztésnél.

## Hibaelhárítás
- Ellenőrizd a whitespace-eket a `.env`-ben (ne legyen sorvégi szóköz pl. a `POSTGRES_USER` végén).
- `docker compose config` mutatja a feloldott értékeket és jelzi, ha hiányzik valami.
- Az API logok: konténer naplók `docker logs <container_name>` (pl. `lostandfound-api`).

## Biztonság
- Soha ne commitold a titkokat (`.env`, `docker-compose.override.yml`).
- A `.env.example` csak mintát tartalmaz, nem valós titkokat.
- A `JWT__SECRET` legyen erős (legalább 32-64 byte), és csak futtatási környezetben legyen megadva.
