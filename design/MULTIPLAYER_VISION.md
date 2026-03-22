# StockSim Multiplayer — Vision Notes (Phase 2+)

> Dieses Dokument ist eine Ideensammlung für den Multiplayer-Modus. Wird erst designed wenn Singleplayer-Verkäufe die Server-Infrastruktur finanzieren.

## Kernideen

- **Echtzeit** — keine Zeitsteuerung, der Markt läuft für alle gleich
- **Ein geteilter Markt** — alle Spieler handeln im gleichen Markt
- **Komprimierte Zeit** — 1 Handelstag = 30-60 Minuten Echtzeit
- **Server läuft 24/7** — auch wenn Spieler offline sind (AI + andere Spieler handeln weiter)
- **Stop-Losses/Limit-Orders arbeiten offline**

## Social Features

- **"The Floor"** — In-Game Reddit/Forum: Posts, DD, YOLO, Loss Porn, Upvotes
- **Trading Groups / Clans** — Koordinierte Aktionen, Gruppen-Portfolio, Rivalen
- **Leaderboards** — Top Traders, Top Groups, Biggest YOLO, Loss Porn Hall of Fame
- **Spieler-Profile** — Performance-Chart, Badges, Trading-Stil
- **Follow / Copy Trading** — Anderen Spielern folgen (15 Min Delay)
- **Trade Challenges** — 1v1 oder Gruppe, wer macht mehr Gewinn

## Twitch/YouTube-Integration

- Chat Votes (Buy/Sell/Short)
- Viewer Predictions
- Chaos Mode (Zuschauer triggern Events)
- Portfolio OBS-Overlay
- Trade-Alerts als Stream-Alerts
- Shared Universe (Zuschauer spielen im gleichen Server)

## Spieler-getriebene Dynamiken

- Koordinierte Pump & Dumps über The Floor
- Spieler-organisierte Short Squeezes
- Insider-Info teilen in Gruppen
- Posts die den Markt bewegen (Reputation-System)
- SMA bestraft echte Spieler die manipulieren

## Architektur-Änderung

- Singleplayer: lokales C# Backend, WebSocket localhost
- Multiplayer: zentrale Server, Spieler verbinden über Internet
- Server-Kosten sollen durch Singleplayer-Verkäufe finanziert werden

## Business Model Multiplayer

- Singleplayer kaufen → finanziert Server
- Kein Pay-to-Win
- Mögliche Monetarisierung: kosmetische Items (Portfolio-Themes, Logo-Styles, Flair-Badges)
- KEIN Pay-to-Win: keine gekauften Vorteile im Handel

## Priorität

Erst wenn Singleplayer:
1. Released und stabil ist
2. Genug Verkäufe für Server-Budget hat
3. Community-Feedback gesammelt wurde
