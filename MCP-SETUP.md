# MCP Server Setup für Claude Desktop

## Übersicht

Der Dynamics AX 2012 MCP Server ermöglicht Claude Desktop den direkten Zugriff auf Dynamics AX Daten über den BC Proxy.

## Voraussetzungen

1. **BC Proxy läuft in IIS** auf `http://localhost:5100`
2. **.NET 8 SDK** installiert
3. **Claude Desktop** installiert

## Claude Desktop Konfiguration

Die Konfigurationsdatei liegt hier:
```
%APPDATA%\Claude\claude_desktop_config.json
```

### Minimale Konfiguration (empfohlen)

```json
{
  "mcpServers": {
    "dynamics-ax": {
      "command": "dotnet",
      "args": ["run", "--project", "D:/source/repos/Komet.MCP.DynamicsAX2012/src/Komet.MCP.DynamicsAX2012.Server"],
      "env": {
        "BC_PROXY_URL": "http://localhost:5100"
      }
    }
  }
}
```

**Wichtig:**
- **Forward slashes** `/` verwenden (cross-platform kompatibel)
- **Projekt-Ordner** angeben, nicht `.csproj` Datei
- **Nur BC_PROXY_URL** als Environment-Variable nötig

### Pfad anpassen

Ersetze den Pfad mit deinem lokalen Repository-Pfad:
```json
"D:/source/repos/Komet.MCP.DynamicsAX2012/src/Komet.MCP.DynamicsAX2012.Server"
```

## Aktivierung

1. Claude Desktop **komplett beenden** (Task Manager prüfen)
2. `claude_desktop_config.json` bearbeiten
3. Claude Desktop **neu starten**
4. Unten links sollte "dynamics-ax" MCP Server als verbunden angezeigt werden

## Verfügbare MCP Tools

### Customer Tools (BC Proxy)

```
Hole mir Kunde 12345 aus Firma GBL
Suche Kunden mit AccountNum beginnend mit "DE"
```

**Tools:**
- `ax_bc_customer_get` - Einzelnen Kunden abrufen
- `ax_bc_customer_search` - Kunden suchen

### Product Tools (BC Proxy)

```
Zeige mir Produkt 036262R0 aus Firma GBL mit allen Kategorien
Suche Produkte mit ItemId beginnend mit "S6850"
```

**Tools:**
- `ax_bc_product_get` - Einzelnes Produkt abrufen (mit Kategorien)
- `ax_bc_product_search` - Produkte suchen

### Sales Order Tools (BC Proxy)

```
Hole Verkaufsauftrag SO-12345 aus Firma GBL
Suche Verkaufsaufträge für Kunde 12345
```

**Tools:**
- `ax_bc_salesorder_get` - Einzelnen Auftrag abrufen
- `ax_bc_salesorder_search` - Aufträge suchen

### X++ Execution

```
Führe X++ Code aus um [Beschreibung]
```

**Tool:**
- `ax_bc_execute_xpp` - Beliebigen X++ Code ausführen

## Troubleshooting

### MCP Server verbindet nicht

**Developer Console öffnen:** `Ctrl+Shift+I` in Claude Desktop

**Häufige Fehler:**

1. **JSON Parsing Error**
   - Überprüfe JSON-Syntax (keine Kommentare, korrekte Kommas)
   - Online JSON Validator verwenden

2. **Server disconnected**
   - BC Proxy läuft nicht → `Invoke-WebRequest http://localhost:5100/api/health`
   - Falsche BC_PROXY_URL

3. **Command not found**
   - `dotnet` nicht im PATH
   - .NET 8 SDK nicht installiert

### BC Proxy Probleme

```powershell
# BC Proxy Status prüfen
Invoke-WebRequest http://localhost:5100/api/health

# IIS App Pool Status
Get-WebAppPoolState -Name "BCProxyAppPool"

# Wenn nicht läuft
Start-WebAppPool -Name "BCProxyAppPool"
```

### MCP Server neu starten

```powershell
# Claude Desktop beenden
taskkill /F /IM "Claude.exe"

# Neu starten
Start-Process "$env:LOCALAPPDATA\Programs\Claude\Claude.exe"
```

## Environment-Variablen (Optional)

```json
"env": {
  "BC_PROXY_URL": "http://localhost:5100",
  "ASPNETCORE_ENVIRONMENT": "Development"
}
```

**BC_PROXY_URL:** BC Proxy Endpunkt (erforderlich)  
**ASPNETCORE_ENVIRONMENT:** Logging-Level (optional)

## Best Practices

1. **BC Proxy läuft permanent in IIS** → schnellere MCP Responses
2. **Forward slashes** für Pfade → Cross-platform Kompatibilität
3. **Minimale Environment-Variablen** → Einfachere Wartung
4. **Projekt-Ordner statt .csproj** → Kürzere Konfiguration

## Beispiel-Konfiguration mit mehreren MCP Servern

```json
{
  "mcpServers": {
    "dynamics-ax": {
      "command": "dotnet",
      "args": ["run", "--project", "D:/source/repos/Komet.MCP.DynamicsAX2012/src/Komet.MCP.DynamicsAX2012.Server"],
      "env": {
        "BC_PROXY_URL": "http://localhost:5100"
      }
    },
    "windchill": {
      "command": "node",
      "args": ["D:/source/repos/GBL-CREO-WINDCHILL-MCP/dist/index.js"],
      "env": {
        "NODE_TLS_REJECT_UNAUTHORIZED": "0"
      }
    }
  }
}
```

## Siehe auch

- [IIS Deployment Anleitung](deployment/IIS-DEPLOYMENT.md)
- [Schnellstart](deployment/SCHNELLSTART.md)
- [BC Proxy API Dokumentation](postman/README.md)
