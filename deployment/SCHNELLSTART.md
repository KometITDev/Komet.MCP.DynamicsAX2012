# BC Proxy - Schnellstart für Dev-Rechner

## 1. IIS installieren

```powershell
# PowerShell als Administrator öffnen
cd d:\source\repos\Komet.MCP.DynamicsAX2012\deployment

# IIS installieren (dauert ca. 5-10 Minuten)
.\Install-IIS.ps1

# Bei Aufforderung: Neustart durchführen (empfohlen)
```

**Alternativ manuell (Windows 10/11):**
1. Windows-Taste → "Windows-Features ein- oder ausschalten"
2. Aktivieren:
   - ✓ Internetinformationsdienste
   - ✓ Webverwaltungstools → IIS-Verwaltungskonsole
   - ✓ WWW-Dienste → Allgemeine HTTP-Features → Alle
   - ✓ WWW-Dienste → Anwendungsentwicklungsfeatures → ASP.NET 4.8
3. OK → Neustart

## 2. IIS testen

```powershell
# Browser öffnen
start http://localhost
```

Erwartung: IIS Standard-Willkommensseite

## 3. BC Proxy deployen

```powershell
# Im deployment Ordner
.\Deploy-BCProxyToIIS.ps1

# Mit eigenen Einstellungen
.\Deploy-BCProxyToIIS.ps1 -AXAosServer "YOUR-AOS-SERVER"
```

**Das Skript macht automatisch:**
- ✓ Buildet BC Proxy
- ✓ Erstellt IIS Site auf Port 5100
- ✓ Konfiguriert App Pool
- ✓ Setzt Berechtigungen
- ✓ Führt Health Check durch

**WICHTIG - App Pool Authentifizierung:**

Der BC Proxy benötigt einen Account mit AX-Berechtigungen. Nach dem Deployment:

```powershell
# Option 1: Domain-User (empfohlen)
$appcmd = "$env:SystemRoot\System32\inetsrv\appcmd.exe"
& $appcmd set apppool "BCProxyAppPool" /processModel.identityType:SpecificUser
& $appcmd set apppool "BCProxyAppPool" /processModel.userName:"DOMAIN\Username"
& $appcmd set apppool "BCProxyAppPool" /processModel.password:"YourPassword"

# Option 2: ApplicationPoolIdentity (modern)
& $appcmd set apppool "BCProxyAppPool" /processModel.identityType:ApplicationPoolIdentity
# Dann "IIS APPPOOL\BCProxyAppPool" in AX als Benutzer anlegen

# App Pool neu starten
& $appcmd stop apppool "BCProxyAppPool"
& $appcmd start apppool "BCProxyAppPool"
```

**Oder über IIS Manager:**
- Application Pools → BCProxyAppPool → Advanced Settings → Identity → Custom account

## 4. BC Proxy testen

```powershell
# Health Check
Invoke-WebRequest http://localhost:5100/api/health

# Customer abrufen (Beispiel)
Invoke-WebRequest "http://localhost:5100/api/customer/12345?company=GBL"

# Product abrufen (Beispiel)
Invoke-WebRequest "http://localhost:5100/api/product/ITEM001?company=GBL&language=de"
```

## 5. Postman Collection importieren

1. Postman öffnen
2. File → Import
3. Datei auswählen: `d:\source\repos\Komet.MCP.DynamicsAX2012\postman\BCProxy API.postman_collection.json`
4. Environment importieren: `BCProxy-Local.postman_environment.json`
5. Environment aktivieren (rechts oben)

**Fertig!** Der BC Proxy läuft nun auf http://localhost:5100

## Troubleshooting

**IIS startet nicht:**
```powershell
# IIS Service prüfen
Get-Service W3SVC

# Manuell starten
Start-Service W3SVC
```

**BC Proxy HTTP 500 Fehler:**
- Event Viewer prüfen (Windows Logs → Application)
- AX AOS Server Connection prüfen
- web.config in `C:\inetpub\BCProxy\` prüfen

**Port 5100 bereits belegt:**
```powershell
# Anderen Port verwenden
.\Deploy-BCProxyToIIS.ps1 -Port 8080
```

## Weitere Hilfe

- **Vollständige Dokumentation:** `IIS-DEPLOYMENT.md`
- **Postman Anleitung:** `postman\README.md`
