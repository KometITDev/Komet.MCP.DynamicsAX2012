# ========================================
# IIS Installation für BC Proxy
# ========================================
# Dieses Skript installiert IIS mit allen benötigten Features für den BC Proxy
#
# Voraussetzungen:
# - Windows 10/11 Pro oder Windows Server
# - PowerShell als Administrator ausführen

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [bool]$IncludeManagementTools = $true,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipRestart
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
    Write-Info "Rechtsklick auf PowerShell → 'Als Administrator ausführen'"
    exit 1
}

Write-Info "========================================="
Write-Info "IIS Installation für BC Proxy"
Write-Info "========================================="

# Windows Version prüfen
$osInfo = Get-CimInstance Win32_OperatingSystem
Write-Info "Betriebssystem: $($osInfo.Caption)"
Write-Info "Version: $($osInfo.Version)"

# Prüfen ob Windows Server oder Desktop
$isServer = $osInfo.Caption -like "*Server*"

if ($isServer) {
    Write-Info "Windows Server erkannt - verwende Server Manager Features"
} else {
    Write-Info "Windows Desktop erkannt - verwende Windows Optional Features"
}

# IIS Features definieren
$iisFeatures = @(
    # Core IIS
    "IIS-WebServerRole",
    "IIS-WebServer",
    "IIS-CommonHttpFeatures",
    "IIS-HttpErrors",
    "IIS-HttpRedirect",
    "IIS-ApplicationDevelopment",
    
    # Static Content
    "IIS-StaticContent",
    "IIS-DefaultDocument",
    "IIS-DirectoryBrowsing",
    
    # Health and Diagnostics
    "IIS-HealthAndDiagnostics",
    "IIS-HttpLogging",
    "IIS-LoggingLibraries",
    "IIS-RequestMonitor",
    "IIS-HttpTracing",
    
    # Performance
    "IIS-Performance",
    "IIS-HttpCompressionStatic",
    "IIS-HttpCompressionDynamic",
    
    # Security
    "IIS-Security",
    "IIS-RequestFiltering",
    "IIS-BasicAuthentication",
    "IIS-WindowsAuthentication",
    
    # Application Development
    "IIS-NetFxExtensibility45",
    "IIS-ISAPIExtensions",
    "IIS-ISAPIFilter",
    "IIS-ASPNET45",
    
    # Management Tools
    "IIS-ManagementConsole",
    "IIS-ManagementScriptingTools"
)

# Installation durchführen
Write-Info ""
Write-Info "Installiere IIS Features..."
Write-Info "Dies kann einige Minuten dauern..."
Write-Info ""

$installResults = @{
    Success = @()
    Failed = @()
    AlreadyInstalled = @()
}

if ($isServer) {
    # Windows Server - verwende Install-WindowsFeature
    foreach ($feature in $iisFeatures) {
        # Feature-Name für Server anpassen (Web- statt IIS-)
        $serverFeature = $feature -replace "^IIS-", "Web-"
        
        Write-Info "Installiere: $serverFeature..."
        
        try {
            $result = Install-WindowsFeature -Name $serverFeature -IncludeManagementTools:$IncludeManagementTools
            
            if ($result.Success) {
                if ($result.RestartNeeded -eq 'Yes') {
                    Write-Warning "Feature $serverFeature benötigt Neustart"
                }
                $installResults.Success += $serverFeature
                Write-Success "$serverFeature installiert"
            } elseif ($result.FeatureResult | Where-Object { $_.State -eq 'Installed' }) {
                $installResults.AlreadyInstalled += $serverFeature
                Write-Info "$serverFeature bereits installiert"
            } else {
                $installResults.Failed += $serverFeature
                Write-Warning "$serverFeature konnte nicht installiert werden"
            }
        }
        catch {
            $installResults.Failed += $serverFeature
            Write-Warning "Fehler bei $serverFeature : $_"
        }
    }
} else {
    # Windows Desktop - verwende Enable-WindowsOptionalFeature
    foreach ($feature in $iisFeatures) {
        Write-Info "Installiere: $feature..."
        
        try {
            $result = Enable-WindowsOptionalFeature -Online -FeatureName $feature -NoRestart
            
            if ($result.RestartNeeded) {
                Write-Warning "Feature $feature benötigt Neustart"
            }
            
            $installResults.Success += $feature
            Write-Success "$feature installiert"
        }
        catch {
            if ($_.Exception.Message -like "*already enabled*" -or $_.Exception.Message -like "*bereits aktiviert*") {
                $installResults.AlreadyInstalled += $feature
                Write-Info "$feature bereits installiert"
            } else {
                $installResults.Failed += $feature
                Write-Warning "Fehler bei $feature : $_"
            }
        }
    }
}

# ASP.NET Core Hosting Bundle Hinweis
Write-Info ""
Write-Info "========================================="
Write-Info "Optional: ASP.NET Core Hosting Bundle"
Write-Info "========================================="
Write-Info "Falls Sie .NET Core/5+ Apps hosten möchten:"
Write-Info "Download: https://dotnet.microsoft.com/download/dotnet/8.0"
Write-Info "Wählen Sie: 'Hosting Bundle' für Windows"
Write-Info ""

# Zusammenfassung
Write-Info ""
Write-Info "========================================="
Write-Info "Installation abgeschlossen"
Write-Info "========================================="

if ($installResults.Success.Count -gt 0) {
    Write-Success "Erfolgreich installiert: $($installResults.Success.Count) Features"
}

if ($installResults.AlreadyInstalled.Count -gt 0) {
    Write-Info "Bereits installiert: $($installResults.AlreadyInstalled.Count) Features"
}

if ($installResults.Failed.Count -gt 0) {
    Write-Warning "Fehlgeschlagen: $($installResults.Failed.Count) Features"
    Write-Warning "Fehlgeschlagene Features:"
    $installResults.Failed | ForEach-Object { Write-Warning "  - $_" }
}

# IIS Service Status prüfen
Write-Info ""
Write-Info "Prüfe IIS Service Status..."

try {
    $iisService = Get-Service W3SVC -ErrorAction Stop
    
    if ($iisService.Status -eq 'Running') {
        Write-Success "IIS Service (W3SVC) läuft"
    } else {
        Write-Info "Starte IIS Service..."
        Start-Service W3SVC
        Write-Success "IIS Service gestartet"
    }
}
catch {
    Write-Warning "IIS Service konnte nicht geprüft werden: $_"
}

# IIS Manager öffnen
Write-Info ""
Write-Info "IIS Manager öffnen:"
Write-Info "  - Windows: Start → 'inetmgr' eingeben"
Write-Info "  - Oder: Systemsteuerung → Verwaltung → Internetinformationsdienste (IIS)-Manager"

# Neustart-Hinweis
$needsRestart = $false

if ($isServer) {
    # Prüfe ob Neustart benötigt wird
    $pendingReboot = Test-Path "HKLM:\Software\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending"
    if ($pendingReboot) {
        $needsRestart = $true
    }
} else {
    # Bei Desktop-Installation oft Neustart nötig
    $needsRestart = $true
}

if ($needsRestart -and -not $SkipRestart) {
    Write-Info ""
    Write-Warning "========================================="
    Write-Warning "NEUSTART EMPFOHLEN"
    Write-Warning "========================================="
    Write-Warning "Ein Neustart wird empfohlen, um die Installation abzuschließen."
    Write-Info ""
    
    $restart = Read-Host "Möchten Sie jetzt neu starten? (j/n)"
    if ($restart -eq 'j' -or $restart -eq 'J' -or $restart -eq 'y' -or $restart -eq 'Y') {
        Write-Info "Neustart in 10 Sekunden..."
        shutdown /r /t 10 /c "IIS Installation abgeschlossen - Neustart"
    } else {
        Write-Info "Bitte führen Sie den Neustart manuell durch."
    }
}

# Test-URL
Write-Info ""
Write-Info "========================================="
Write-Info "Nächste Schritte"
Write-Info "========================================="
Write-Info "1. IIS testen:"
Write-Info "   - Browser öffnen: http://localhost"
Write-Info "   - Erwartung: IIS Standard-Seite"
Write-Info ""
Write-Info "2. BC Proxy deployen:"
Write-Info "   - .\Deploy-BCProxyToIIS.ps1 ausführen"
Write-Info ""
Write-Success "IIS Installation erfolgreich!"
