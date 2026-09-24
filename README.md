# Shaker Business

Ein Idle-/Clicker-Spiel im Stil von Adventure Capitalist für den Twitch Streamer "Shaker", gebaut mit Blazor Server (.NET 10) und
MariaDB. Login läuft über Twitch, es gibt aber auch einen Gast-Modus ohne Login.

## Voraussetzungen

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Eine MariaDB Datenbank (lokal oder per Docker)
- Ein eigener [Twitch-Dev-App-Eintrag](https://dev.twitch.tv/console/apps) für den Login (optional,
  siehe unten)

## Setup

1. Repo klonen und in den Projektordner wechseln.
2. Eine Datenbank anlegen, z. B. mit Docker:
   ```bash
   docker run -d --name shaker-mariadb -e MARIADB_ROOT_PASSWORD=root -e MARIADB_DATABASE=shakerbusiness -e MARIADB_USER=shakerbusiness -e MARIADB_PASSWORD=change-me -p 3306:3306 mariadb:11
   ```
3. Secrets lokal setzen (landen nicht im Repo, siehe [`appsettings.Example.json`](ShakerBusiness/appsettings.Example.json) für die erwarteten Keys):
   ```bash
   cd ShakerBusiness
   dotnet user-secrets set "ConnectionStrings:MariaDb" "Server=localhost;Port=3306;Database=shakerbusiness;Uid=shakerbusiness;Pwd=change-me;"
   dotnet user-secrets set "Twitch:ClientId" "dein-client-id"
   dotnet user-secrets set "Twitch:ClientSecret" "dein-client-secret"
   ```
4. Starten:
   ```bash
   dotnet run
   ```
   Datenbank-Migrationen werden beim Start automatisch angewendet, dafür ist nichts weiter nötig.
   Tailwind-CSS wird bei jedem Build automatisch aus `wwwroot/input.css` neu erzeugt.

Ohne gesetzte Twitch-Keys startet die Seite trotzdem, man kann dann nur den Gast-Modus nutzen
(Login-Button zeigt einen Hinweis statt eines Twitch-Logins).

## Twitch-Login einrichten

Im [Twitch-Dev-Konsole](https://dev.twitch.tv/console/apps) eine neue App anlegen mit OAuth-Redirect-URL
`http://localhost:5149/signin-twitch` (Port aus `Properties/launchSettings.json`). Client-ID und
-Secret dann wie oben per `dotnet user-secrets` setzen.

## Admin-Zugriff

Es gibt kein automatisches Setup für den ersten Admin-Account. Nach dem ersten Login per Twitch
direkt in der Datenbank den eigenen Account als Dev markieren:

```sql
UPDATE PlayerAccounts SET IsDev = 1 WHERE TwitchUserId = 'deine-twitch-user-id';
```

Damit erscheint der Link "Admin Tool" in der Seitenleiste, und Wartungsmodus, globale
Umsatz-Boosts, Spielende-Termin usw. lassen sich dort steuern.

## Lizenz

[MIT](LICENSE)
