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

function Download-Self {
    Invoke-RestMethod -OutFile $scriptPath -Uri "${githubBaseUrl}/scripts/bravura.ps1"
}

function Download-Run-File {
    if (!(Test-Path -Path $scriptsDir)) {
        New-Item -ItemType directory -Path $scriptsDir | Out-Null
    }
    Invoke-RestMethod -OutFile $scriptsDir\run.ps1 -Uri "${githubBaseUrl}/scripts/run.ps1"
}

function Check-Output-Dir-Exists {
    if (!(Test-Path -Path $output)) {
        throw "Cannot find a Bravura Pass installation at $output."
    }
}

function Check-Output-Dir-Not-Exists {
    if (Test-Path -Path "$output\docker") {
        throw "Looks like Bravura Pass is already installed at $output."
    }
}

function List-Commands {
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
Bravura Pass Vault
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
    Check-Output-Dir-Not-Exists
    New-Item -ItemType directory -Path $output -ErrorAction Ignore | Out-Null
    Download-Run-File
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -install -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($start -Or $restart) {
    Check-Output-Dir-Exists
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -restart -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($update) {
    Check-Output-Dir-Exists
    Download-Run-File
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -update -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($rebuild) {
    Check-Output-Dir-Exists
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -rebuild -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($updateconf) {
    Check-Output-Dir-Exists
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -updateconf -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($updatedb) {
    Check-Output-Dir-Exists
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -updatedb -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($stop) {
    Check-Output-Dir-Exists
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -stop -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($renewcert) {
    Check-Output-Dir-Exists
    Invoke-Expression "& `"$scriptsDir\run.ps1`" -renewcert -outputDir `"$output`" -coreVersion $coreVersion -webVersion $webVersion"
}
elseif ($updaterun) {
    Check-Output-Dir-Exists
    Download-Run-File
}
elseif ($updateself) {
    Download-Self
    Write-Line "Updated self."
}
elseif ($help) {
    List-Commands
}
else {
    Write-Line "No command found."
    Write-Line ""
    List-Commands
}
