# IIS Deployment Anleitung - BC Proxy

## Übersicht

Diese Anleitung beschreibt das Deployment des Dynamics AX 2012 Business Connector Proxy auf einem IIS Server im Netzwerk.

## Voraussetzungen

### Server-Anforderungen
- **Windows Server** 2012 R2 oder höher
- **IIS** (Internet Information Services) installiert
- **.NET Framework 4.8** installiert
- **Dynamics AX 2012 Business Connector** installiert
- Zugriff auf AX AOS Server (Dynamics AX Application Object Server)

### IIS Features
Folgende IIS Features müssen aktiviert sein:
```powershell
# PowerShell als Administrator ausführen
Install-WindowsFeature Web-Server,Web-WebServer,Web-Common-Http,Web-Default-Doc,Web-Dir-Browsing,Web-Http-Errors,Web-Static-Content,Web-Http-Redirect,Web-Health,Web-Http-Logging,Web-Log-Libraries,Web-Request-Monitor,Web-Performance,Web-Stat-Compression,Web-Security,Web-Filtering,Web-Basic-Auth,Web-Windows-Auth,Web-Net-Ext45,Web-Asp-Net45,Web-ISAPI-Ext,Web-ISAPI-Filter,Web-Mgmt-Tools,Web-Mgmt-Console
```

### Business Connector Installation
Der BC Proxy benötigt die folgenden DLLs:
- `Microsoft.Dynamics.BusinessConnectorNet.dll`
- `Microsoft.Dynamics.AX.ManagedInterop.dll`

Standard-Pfad: `C:\Program Files\Microsoft Dynamics AX\60\BusinessConnector\Bin\`

## Automatisches Deployment

### 1. PowerShell Skript verwenden

```powershell
# PowerShell als Administrator öffnen
cd d:\source\repos\Komet.MCP.DynamicsAX2012\deployment

# Deployment mit Standard-Einstellungen
.\Deploy-BCProxyToIIS.ps1

# Deployment mit benutzerdefinierten Einstellungen
.\Deploy-BCProxyToIIS.ps1 `
    -SiteName "BCProxy" `
    -Port 5100 `
    -PhysicalPath "C:\inetpub\BCProxy" `
    -AXAosServer "YOUR-AOS-SERVER" `
    -AXSqlConnection "Server=YOUR-SQL-SERVER;Database=MicrosoftDynamicsAX;Integrated Security=True;"
```

### Parameter

| Parameter | Beschreibung | Default |
|-----------|--------------|---------|
| `SiteName` | Name der IIS Website | BCProxy |
| `AppPoolName` | Name des Application Pools | BCProxyAppPool |
| `Port` | HTTP Port | 5100 |
| `PhysicalPath` | Deployment-Verzeichnis | C:\inetpub\BCProxy |
| `SourcePath` | Pfad zum Quellcode (optional) | Auto-erkannt |
| `AXAosServer` | AX AOS Server Name | IT-TEST-ERP3CU |
| `AXSqlConnection` | AX SQL Connection String | [siehe Skript] |

## App Pool Authentifizierung für AX Zugriff

**WICHTIG:** Der BC Proxy benötigt einen Account mit Dynamics AX Berechtigungen, um sich beim Business Connector anzumelden.

### Option 1: Domain-User (Empfohlen für Produktion)

Verwende einen Domain-User mit AX-Berechtigungen:

```powershell
$appcmd = "$env:SystemRoot\System32\inetsrv\appcmd.exe"

# Domain-User konfigurieren
& $appcmd set apppool "BCProxyAppPool" /processModel.identityType:SpecificUser
& $appcmd set apppool "BCProxyAppPool" /processModel.userName:"DOMAIN\Username"
& $appcmd set apppool "BCProxyAppPool" /processModel.password:"YourPassword"

# App Pool neu starten
& $appcmd stop apppool "BCProxyAppPool"
& $appcmd start apppool "BCProxyAppPool"
```

**Oder über IIS Manager:**
1. IIS Manager → Application Pools → BCProxyAppPool
2. Rechtsklick → Advanced Settings
3. Process Model → Identity → Custom account
4. Set Credentials eingeben
5. OK → App Pool neu starten

**Vorteile:**
- Explizite Kontrolle über Berechtigungen
- Einfaches Troubleshooting
- Standard für Produktionsumgebungen

### Option 2: ApplicationPoolIdentity (Modern)

```powershell
& $appcmd set apppool "BCProxyAppPool" /processModel.identityType:ApplicationPoolIdentity
```

**Wichtig:** Account `IIS APPPOOL\BCProxyAppPool` muss in AX als Benutzer angelegt werden.

**In Dynamics AX:**
1. System Administration → Common → Users
2. Create users from Windows logins
3. Account hinzufügen: `IIS APPPOOL\BCProxyAppPool`
4. User-Rolle zuweisen mit Business Connector Berechtigung

**Vorteile:**
- Bessere Sicherheit (isolierter Account pro App Pool)
- Keine Passwort-Verwaltung nötig

### Option 3: NetworkService (Standard, nicht empfohlen)

```powershell
& $appcmd set apppool "BCProxyAppPool" /processModel.identityType:NetworkService
```

**Wichtig:** Account `NT AUTHORITY\NETWORK SERVICE` muss in AX berechtigt werden.

**In Dynamics AX:**
1. System Administration → Common → Users
2. Create users from Windows logins
3. Account hinzufügen: `NT AUTHORITY\NETWORK SERVICE`
4. User-Rolle zuweisen

**Nachteile:**
- Geteilter Account (mehrere Dienste nutzen NetworkService)
- Schwieriger zu auditen

### AX-Berechtigungen prüfen

```powershell
# Test nach App Pool Änderung
Invoke-WebRequest "http://localhost:5100/api/health"
Invoke-WebRequest "http://localhost:5100/api/product/ITEM001?company=GBL"
```

**Bei Fehler "Unable to log on to Microsoft Dynamics AX":**
- App Pool Identity prüfen
- AX-Benutzer und Berechtigungen prüfen
- Business Connector Konfiguration prüfen

## Manuelles Deployment

Falls das PowerShell Skript nicht verwendet werden kann:

### 1. Projekt builden

```powershell
cd src\Komet.MCP.DynamicsAX2012.BCProxy
dotnet clean
dotnet publish -c Release -o bin\Release\net48\publish --framework net48
```

### 2. IIS Application Pool erstellen

1. IIS Manager öffnen
2. Rechtsklick auf "Application Pools" → "Add Application Pool"
3. **Name:** BCProxyAppPool
4. **.NET CLR Version:** v4.0
5. **Managed pipeline mode:** Integrated
6. **Start immediately:** aktiviert

**App Pool Konfiguration:**
- **Identity:** NetworkService
- **Enable 32-Bit Applications:** False
- **Start Mode:** AlwaysRunning
- **Load User Profile:** True

### 3. IIS Website erstellen

1. Rechtsklick auf "Sites" → "Add Website"
2. **Site name:** BCProxy
3. **Physical path:** C:\inetpub\BCProxy
4. **Application pool:** BCProxyAppPool
5. **Binding:**
   - Type: http
   - IP address: All Unassigned
   - Port: 5100

### 4. Dateien kopieren

Kopiere alle Dateien aus `bin\Release\net48\publish\` nach `C:\inetpub\BCProxy\`

### 5. web.config erstellen

Erstelle `C:\inetpub\BCProxy\web.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <add key="AX_AOS_SERVER" value="YOUR-AOS-SERVER" />
    <add key="AX_SQL_CONNECTION" value="Server=YOUR-SQL-SERVER;Database=MicrosoftDynamicsAX;Integrated Security=True;" />
  </appSettings>
  
  <system.web>
    <compilation debug="false" targetFramework="4.8" />
    <httpRuntime targetFramework="4.8" maxRequestLength="102400" executionTimeout="300" />
    <customErrors mode="Off" />
  </system.web>
  
  <system.webServer>
    <handlers>
      <add name="Owin" verb="*" path="*" type="Microsoft.Owin.Host.SystemWeb.OwinHttpHandler, Microsoft.Owin.Host.SystemWeb" />
    </handlers>
    <modules runAllManagedModulesForAllRequests="true" />
    <directoryBrowse enabled="false" />
  </system.webServer>
  
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
        <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-13.0.0.0" newVersion="13.0.0.0" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>
```

### 6. Berechtigungen setzen

```powershell
# Auf C:\inetpub\BCProxy
icacls "C:\inetpub\BCProxy" /grant "IIS_IUSRS:(OI)(CI)RX"
icacls "C:\inetpub\BCProxy" /grant "NETWORK SERVICE:(OI)(CI)M"
```

## Konfiguration

### Environment Variablen

Die Konfiguration erfolgt über `web.config` AppSettings:

```xml
<appSettings>
  <!-- AX AOS Server Name -->
  <add key="AX_AOS_SERVER" value="YOUR-AOS-SERVER" />
  
  <!-- SQL Connection String für AX Datenbank -->
  <add key="AX_SQL_CONNECTION" value="Server=YOUR-SQL;Database=MicrosoftDynamicsAX;Integrated Security=True;" />
</appSettings>
```

### Firewallregeln

Falls der BC Proxy von anderen Servern erreichbar sein soll:

```powershell
New-NetFirewallRule -DisplayName "BCProxy HTTP" `
    -Direction Inbound `
    -LocalPort 5100 `
    -Protocol TCP `
    -Action Allow
```

## Testen

### Health Check

```powershell
# Lokal
Invoke-WebRequest -Uri "http://localhost:5100/api/health"

# Remote
Invoke-WebRequest -Uri "http://YOUR-SERVER:5100/api/health"
```

**Erwartete Response:**
```json
{
  "service": "Dynamics AX 2012 Business Connector Proxy",
  "version": "1.3.0.0",
  "timestamp": "2024-12-16T09:00:00Z",
  "platform": ".NET Framework 4.8",
  "status": "healthy"
}
```

### API Endpunkte testen

```powershell
# Customer abrufen
Invoke-WebRequest -Uri "http://localhost:5100/api/customer/12345?company=GBL"

# Product abrufen
Invoke-WebRequest -Uri "http://localhost:5100/api/product/ITEM001?company=GBL&language=de"

# Sales Order abrufen
Invoke-WebRequest -Uri "http://localhost:5100/api/salesorder/SO-12345?company=GBL"
```

## Troubleshooting

### BC Proxy startet nicht

1. **Event Viewer prüfen:**
   - Windows Logs → Application
   - Suche nach Fehlern von "BCProxy" oder "IIS"

2. **IIS Logs prüfen:**
   - `C:\inetpub\logs\LogFiles\W3SVC*\`

3. **Business Connector Installation prüfen:**
   ```powershell
   Test-Path "C:\Program Files\Microsoft Dynamics AX\60\BusinessConnector\Bin\Microsoft.Dynamics.BusinessConnectorNet.dll"
   ```

### HTTP 500 Fehler

**Häufige Ursachen:**
- **AX AOS Server nicht erreichbar** → Connection String prüfen
- **SQL Connection fehlgeschlagen** → Connection String prüfen
- **Fehlende DLLs** → Business Connector Installation prüfen
- **Berechtigungsprobleme** → App Pool Identity prüfen

**Debugging aktivieren:**
```xml
<!-- In web.config -->
<system.web>
  <customErrors mode="Off" />
  <compilation debug="true" targetFramework="4.8" />
</system.web>
```

### HTTP 503 Service Unavailable

**Ursachen:**
- App Pool gestoppt
- App Pool Crash wegen fehlendem .NET Framework

**Lösung:**
```powershell
# App Pool neu starten
Restart-WebAppPool -Name "BCProxyAppPool"

# Status prüfen
Get-WebAppPoolState -Name "BCProxyAppPool"
```

### AX Connection Fehler

**Symptom:** "Error logging into AX"

**Prüfungen:**
1. AOS Server erreichbar: `Test-NetConnection -ComputerName AOS-SERVER -Port 2712`
2. SQL Server erreichbar: `Test-NetConnection -ComputerName SQL-SERVER -Port 1433`
3. App Pool Identity hat AX-Berechtigungen
4. Firewall-Regeln auf AOS Server prüfen

### Performance Probleme

**App Pool Tuning:**
```powershell
# Recycling deaktivieren (für Produktion)
Set-ItemProperty "IIS:\AppPools\BCProxyAppPool" -Name recycling.periodicRestart.time -Value "00:00:00"

# Idle Timeout erhöhen
Set-ItemProperty "IIS:\AppPools\BCProxyAppPool" -Name processModel.idleTimeout -Value "00:20:00"

# Request Queue erhöhen
Set-ItemProperty "IIS:\AppPools\BCProxyAppPool" -Name queueLength -Value 5000
```

## Wartung

### Update durchführen

1. Neuen Build erstellen
2. Deployment-Skript erneut ausführen (überschreibt Dateien automatisch)
3. App Pool recyclen falls nötig:
   ```powershell
   Restart-WebAppPool -Name "BCProxyAppPool"
   ```

### Logs überwachen

**IIS Logs:**
```powershell
Get-Content "C:\inetpub\logs\LogFiles\W3SVC*\*.log" -Tail 50 -Wait
```

**Windows Event Log:**
```powershell
Get-EventLog -LogName Application -Source "BCProxy" -Newest 50
```

## Sicherheit

### HTTPS aktivieren (Produktion)

1. **SSL Zertifikat** installieren
2. **HTTPS Binding** hinzufügen:
   ```powershell
   New-WebBinding -Name "BCProxy" -Protocol https -Port 443 -SslFlags 0
   ```
3. **HTTP → HTTPS Redirect** in web.config:
   ```xml
   <system.webServer>
     <rewrite>
       <rules>
         <rule name="HTTP to HTTPS redirect" stopProcessing="true">
           <match url="(.*)" />
           <conditions>
             <add input="{HTTPS}" pattern="off" ignoreCase="true" />
           </conditions>
           <action type="Redirect" url="https://{HTTP_HOST}/{R:1}" redirectType="Permanent" />
         </rule>
       </rules>
     </rewrite>
   </system.webServer>
   ```

### Authentifizierung

Für Produktionsumgebungen empfohlen:

1. **Windows Authentication** in IIS aktivieren
2. **Anonymous Authentication** deaktivieren
3. **API Key Middleware** implementieren (optional)

## Monitoring

### Health Check Endpoint

Der `/api/health` Endpoint kann für Monitoring verwendet werden:

```powershell
# Monitoring Skript (alle 60 Sekunden)
while ($true) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5100/api/health" -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host "$(Get-Date) - OK" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "$(Get-Date) - FAILED: $_" -ForegroundColor Red
        # Alert senden
    }
    Start-Sleep -Seconds 60
}
```

## Support

Bei Problemen:
1. Event Viewer prüfen
2. IIS Logs analysieren
3. AX AOS Server Verbindung testen
4. Business Connector Installation verifizieren

**Version:** 1.3.0  
**Letzte Aktualisierung:** Dezember 2024
