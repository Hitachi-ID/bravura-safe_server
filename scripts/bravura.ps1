param (
    [switch] $install,
    [switch] $start,
    [switch] $restart,
    [switch] $stop,
    [switch] $update,
    [switch] $rebuild,
    [switch] $updateconf,
    [switch] $renewcert,
    [switch] $updatedb,
    [switch] $updaterun,
    [switch] $updateself,
    [switch] $help,
    [string] $output = ""
)

# Setup

$scriptPath = $MyInvocation.MyCommand.Path
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ($output -eq "") {
    $output = "${dir}\bvdata"
}

$scriptsDir = "${output}\scripts"
$githubBaseUrl = "https://gitlab.hitachi-id.com/bravura-vault/server"

# Please do not create pull requests modifying the version numbers.
$coreVersion = "latest"
$webVersion = "latest"

# Functions

function Get-Self {
    Invoke-RestMethod -OutFile $scriptPath -Uri "${githubBaseUrl}/scripts/bravura.ps1"
}

function Get-Run-File {
    if (!(Test-Path -Path $scriptsDir)) {
        New-Item -ItemType directory -Path $scriptsDir | Out-Null
    }
    # Until we have a published, public place to stash this, manually copy run.sh with bravura.sh and use that file
    # Invoke-RestMethod -OutFile $scriptsDir\run.ps1 -Uri "${githubBaseUrl}/scripts/run.ps1"
    Copy-Item "run.ps1" -Destination $scriptsDir\run.ps1
}

function Test-Output-Dir-Exists {
    if (!(Test-Path -Path $output)) {
        throw "Cannot find a Bravura Pass installation at $output."
    }
}

function Test-Output-Dir-Not-Exists {
    if (Test-Path -Path "$output\docker") {
        throw "Looks like Bravura Pass is already installed at $output."
    }
}

function Show-Commands {
    Write-Line "
Available commands:

-install
-start
-restart
-stop
-update
-updatedb
-updaterun
-updateself
-updateconf
-renewcert
-rebuild
-help
"
}

function Write-Line($str) {
    if($env:BRAVURA_VAULT_QUIET -ne "true") {
        Write-Host $str
    }
}

# Intro

$year = (Get-Date).year

Write-Line @'
HITACHI-ID
Bravura Safe Vault
'@

Write-Line "
Open source password management solutions
===================================================
"

if($env:BITWARDEN_QUIET -ne "true") {
    Write-Line "Bravura.ps1 version ${coreVersion}"
    docker --version
    docker-compose --version
}

Write-Line ""

# Commands

if ($install) {
    Test-Output-Dir-Not-Exists
    New-Item -ItemType directory -Path $output -ErrorAction Ignore | Out-Null
    Get-Run-File
    #Invoke-Expression "& `"$scriptsDir\run.ps1`" -install -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($start -Or $restart) {
    Test-Output-Dir-Exists
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -restart -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($update) {
    Test-Output-Dir-Exists
    Get-Run-File
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -update -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($rebuild) {
    Test-Output-Dir-Exists
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -rebuild -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($updateconf) {
    Test-Output-Dir-Exists
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -updateconf -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($updatedb) {
    Test-Output-Dir-Exists
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -updatedb -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($stop) {
    Test-Output-Dir-Exists
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -stop -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($renewcert) {
    Test-Output-Dir-Exists
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -renewcert -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($updaterun) {
    Test-Output-Dir-Exists
    Get-Run-File
}
elseif ($updateself) {
    # Until we have a published, public place to stash this, manually retrieve bravura.sh
    # Download-Self
    # Write-Line "Updated self."
    Write-Line "Please manually retrieve latest file."
}
elseif ($help) {
    Show-Commands
}
else {
    Write-Line "No command found."
    Write-Line ""
    Show-Commands
}
