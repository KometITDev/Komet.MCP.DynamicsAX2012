# ========================================
# BC Proxy IIS Deployment Script
# ========================================
# Dieses Skript deployt den Dynamics AX 2012 Business Connector Proxy auf IIS
#
# Voraussetzungen:
# - IIS installiert mit .NET Framework 4.8 Support
# - Dynamics AX 2012 Business Connector installiert
# - PowerShell als Administrator ausführen
# - ASP.NET Core Hosting Bundle (optional, falls .NET 8 benötigt wird)

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$SiteName = "BCProxy",
    
    [Parameter(Mandatory=$false)]
    [string]$AppPoolName = "BCProxyAppPool",
    
    [Parameter(Mandatory=$false)]
    [int]$Port = 5100,
    
    [Parameter(Mandatory=$false)]
    [string]$PhysicalPath = "C:\inetpub\BCProxy",
    
    [Parameter(Mandatory=$false)]
    [string]$SourcePath = $null,
    
    [Parameter(Mandatory=$false)]
    [string]$AXAosServer = "IT-TEST-ERP3CU",
    
    [Parameter(Mandatory=$false)]
    [string]$AXSqlConnection = "Server=IT-TEST-ERP3CU;Database=MicrosoftDynamicsGBLAX;Integrated Security=True;"
)

# Farben für Output
function Write-Success { param($Message) Write-Host "✓ $Message" -ForegroundColor Green }
function Write-Info { param($Message) Write-Host "ℹ $Message" -ForegroundColor Cyan }
function Write-Warning { param($Message) Write-Host "⚠ $Message" -ForegroundColor Yellow }
function Write-ErrorMsg { param($Message) Write-Host "✗ $Message" -ForegroundColor Red }

# Admin-Rechte prüfen
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-ErrorMsg "Dieses Skript muss als Administrator ausgeführt werden!"
    exit 1
}

Write-Info "========================================="
Write-Info "BC Proxy IIS Deployment"
Write-Info "========================================="
Write-Info "Site Name: $SiteName"
Write-Info "App Pool: $AppPoolName"
Write-Info "Port: $Port"
Write-Info "Physical Path: $PhysicalPath"
Write-Info "AX AOS Server: $AXAosServer"
Write-Info "========================================="

# WebAdministration Modul laden
Import-Module WebAdministration -ErrorAction Stop

# 1. Source Path bestimmen
if ([string]::IsNullOrEmpty($SourcePath)) {
    $scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
    $SourcePath = Join-Path (Split-Path -Parent $scriptPath) "src\Komet.MCP.DynamicsAX2012.BCProxy"
}

Write-Info "Source Path: $SourcePath"

if (-not (Test-Path $SourcePath)) {
    Write-ErrorMsg "Source Path nicht gefunden: $SourcePath"
    exit 1
}

# 2. Projekt builden
Write-Info "Building BC Proxy..."
$publishPath = Join-Path $SourcePath "bin\Release\net48\publish"

try {
    Push-Location $SourcePath
    
    # Clean
    if (Test-Path "bin") {
        Remove-Item -Path "bin" -Recurse -Force
    }
    if (Test-Path "obj") {
        Remove-Item -Path "obj" -Recurse -Force
    }
    
    # Build und Publish
    dotnet publish -c Release -o $publishPath --framework net48
    
    if ($LASTEXITCODE -ne 0) {
        throw "Build fehlgeschlagen"
    }
    
    Write-Success "Build erfolgreich"
    Pop-Location
}
catch {
    Write-ErrorMsg "Build Error: $_"
    Pop-Location
    exit 1
}

# 3. Physical Path erstellen
Write-Info "Erstelle Deployment-Verzeichnis: $PhysicalPath"
if (-not (Test-Path $PhysicalPath)) {
    New-Item -Path $PhysicalPath -ItemType Directory -Force | Out-Null
    Write-Success "Verzeichnis erstellt"
}

# 4. Dateien kopieren mit korrekter Ordnerstruktur
Write-Info "Kopiere Dateien nach $PhysicalPath..."
try {
    # Alte Dateien löschen (außer web.config falls vorhanden)
    if (Test-Path $PhysicalPath) {
        $webConfigBackup = $null
        $webConfigPath = Join-Path $PhysicalPath "web.config"
        
        if (Test-Path $webConfigPath) {
            $webConfigBackup = Get-Content $webConfigPath -Raw
            Write-Info "web.config gesichert"
        }
        
        Get-ChildItem -Path $PhysicalPath -Recurse | Remove-Item -Force -Recurse
        
        if ($webConfigBackup) {
            Set-Content -Path $webConfigPath -Value $webConfigBackup
            Write-Info "web.config wiederhergestellt"
        }
    }
    
    # bin-Ordner erstellen
    $binPath = Join-Path $PhysicalPath "bin"
    if (-not (Test-Path $binPath)) {
        New-Item -Path $binPath -ItemType Directory -Force | Out-Null
    }
    
    # DLLs und PDBs in bin-Ordner kopieren
    Get-ChildItem -Path $publishPath -Filter "*.dll" | Copy-Item -Destination $binPath -Force
    Get-ChildItem -Path $publishPath -Filter "*.pdb" | Copy-Item -Destination $binPath -Force
    
    # Config-Dateien ins Root kopieren (falls vorhanden)
    Get-ChildItem -Path $publishPath -Filter "*.config" | Copy-Item -Destination $PhysicalPath -Force
    
    Write-Success "Dateien kopiert (DLLs in bin-Ordner)"
}
catch {
    Write-ErrorMsg "Fehler beim Kopieren: $_"
    exit 1
}

# 5. web.config erstellen/aktualisieren
Write-Info "Erstelle web.config..."
$webConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
    <add key="AX_AOS_SERVER" value="$AXAosServer" />
    <add key="AX_SQL_CONNECTION" value="$AXSqlConnection" />
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
      <dependentAssembly>
        <assemblyIdentity name="Microsoft.Owin" publicKeyToken="31bf3856ad364e35" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-4.2.2.0" newVersion="4.2.2.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="Microsoft.Owin.Host.SystemWeb" publicKeyToken="31bf3856ad364e35" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-4.2.2.0" newVersion="4.2.2.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Web.Http" publicKeyToken="31bf3856ad364e35" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-5.3.0.0" newVersion="5.3.0.0" />
      </dependentAssembly>
      <dependentAssembly>
        <assemblyIdentity name="System.Net.Http.Formatting" publicKeyToken="31bf3856ad364e35" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-5.3.0.0" newVersion="5.3.0.0" />
      </dependentAssembly>
    </assemblyBinding>
  </runtime>
</configuration>
"@

Set-Content -Path (Join-Path $PhysicalPath "web.config") -Value $webConfigContent -Force
Write-Success "web.config erstellt"

# 6. Application Pool erstellen/aktualisieren
Write-Info "Konfiguriere Application Pool: $AppPoolName"
try {
    # Prüfen ob App Pool existiert (nur Get-WebAppPoolState nutzen)
    $poolExists = $false
    try {
        $state = Get-WebAppPoolState -Name $AppPoolName -ErrorAction Stop
        $poolExists = $true
        Write-Info "App Pool existiert bereits (Status: $($state.Value))"
        
        # Stoppen falls läuft
        if ($state.Value -eq 'Started') {
            Stop-WebAppPool -Name $AppPoolName
            Start-Sleep -Seconds 2
        }
    }
    catch {
        Write-Info "Erstelle neuen App Pool"
        $poolExists = $false
    }
    
    if (-not $poolExists) {
        New-WebAppPool -Name $AppPoolName | Out-Null
        Start-Sleep -Seconds 1
    }
    
    # Einstellungen mit appcmd.exe konfigurieren (robuster als IIS PSDrive)
    $appcmd = "$env:SystemRoot\System32\inetsrv\appcmd.exe"
    
    & $appcmd set apppool $AppPoolName /managedRuntimeVersion:v4.0 | Out-Null
    & $appcmd set apppool $AppPoolName /enable32BitAppOnWin64:false | Out-Null
    & $appcmd set apppool $AppPoolName /processModel.identityType:NetworkService | Out-Null
    & $appcmd set apppool $AppPoolName /processModel.loadUserProfile:true | Out-Null
    & $appcmd set apppool $AppPoolName /startMode:AlwaysRunning | Out-Null
    
    Write-Success "App Pool konfiguriert"
}
catch {
    Write-ErrorMsg "Fehler beim Konfigurieren des App Pools: $_"
    Write-ErrorMsg "Details: $($_.Exception.Message)"
    exit 1
}

# 7. IIS Site erstellen
Write-Info "Erstelle IIS Site: $SiteName"
try {
    if (Test-Path "IIS:\Sites\$SiteName") {
        Write-Warning "Site existiert bereits, wird entfernt"
        Remove-Website -Name $SiteName
    }
    
    New-Website -Name $SiteName `
                -PhysicalPath $PhysicalPath `
                -ApplicationPool $AppPoolName `
                -Port $Port `
                -Force
    
    Write-Success "IIS Site erstellt"
}
catch {
    Write-ErrorMsg "Fehler beim Erstellen der Website: $_"
    exit 1
}

# 8. Berechtigungen setzen
Write-Info "Setze Dateisystem-Berechtigungen..."
try {
    $acl = Get-Acl $PhysicalPath
    
    # IIS_IUSRS
    $iisUsersRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.AddAccessRule($iisUsersRule)
    
    # NetworkService (für App Pool)
    $networkServiceRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "NETWORK SERVICE", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.AddAccessRule($networkServiceRule)
    
    Set-Acl -Path $PhysicalPath -AclObject $acl
    Write-Success "Berechtigungen gesetzt"
}
catch {
    Write-Warning "Berechtigungen konnten nicht gesetzt werden: $_"
}

# 9. App Pool starten
Write-Info "Starte Application Pool..."
Start-WebAppPool -Name $AppPoolName
Write-Success "App Pool gestartet"

# 10. Website starten
Write-Info "Starte Website..."
Start-Website -Name $SiteName
Write-Success "Website gestartet"

# 11. Health Check
Write-Info "Führe Health Check durch..."
Start-Sleep -Seconds 3

try {
    $healthUrl = "http://localhost:$Port/api/health"
    $response = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 10
    
    if ($response.StatusCode -eq 200) {
        Write-Success "Health Check erfolgreich - BC Proxy läuft!"
        Write-Info "BC Proxy URL: http://localhost:$Port"
        Write-Info "Health URL: $healthUrl"
    }
    else {
        Write-Warning "Health Check fehlgeschlagen (Status: $($response.StatusCode))"
    }
}
catch {
    Write-Warning "Health Check konnte nicht durchgeführt werden: $_"
    Write-Info "Bitte prüfen Sie die IIS-Logs und Event Viewer"
}

# 12. Zusammenfassung
Write-Info ""
Write-Info "========================================="
Write-Success "Deployment abgeschlossen!"
Write-Info "========================================="
Write-Info "Site Name: $SiteName"
Write-Info "URL: http://localhost:$Port"
Write-Info "Physical Path: $PhysicalPath"
Write-Info "App Pool: $AppPoolName"
Write-Info ""
Write-Info "Verfügbare Endpunkte:"
Write-Info "  - GET  http://localhost:$Port/api/health"
Write-Info "  - GET  http://localhost:$Port/api/customer/{accountNum}"
Write-Info "  - GET  http://localhost:$Port/api/product/{itemId}"
Write-Info "  - GET  http://localhost:$Port/api/salesorder/{salesId}"
Write-Info "  - POST http://localhost:$Port/api/ax/execute"
Write-Info ""
Write-Info "Environment Variablen:"
Write-Info "  - AX_AOS_SERVER: $AXAosServer"
Write-Info "  - AX_SQL_CONNECTION: [konfiguriert]"
Write-Info ""
Write-Info "Firewall-Regel hinzufügen (falls nötig):"
Write-Info "  New-NetFirewallRule -DisplayName 'BCProxy' -Direction Inbound -LocalPort $Port -Protocol TCP -Action Allow"
Write-Info "========================================="
