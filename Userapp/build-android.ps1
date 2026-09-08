param(
    [switch]$SkipInstall
)

$ErrorActionPreference = 'Stop'
$projectDirectory = $PSScriptRoot
$projectFile = Join-Path $projectDirectory 'CGM.PatientApp.csproj'
$apkPath = Join-Path $projectDirectory 'bin\Debug\net10.0-android\com.companyname.cgm.patientapp-Signed.apk'
$applicationId = 'com.companyname.cgm.patientapp'
$buildStartedAt = Get-Date

Write-Host 'Stopping stale .NET compiler servers...'
& dotnet build-server shutdown
if ($LASTEXITCODE -ne 0) { throw 'Could not stop the .NET build servers.' }

# Roslyn can occasionally survive build-server shutdown and keep obj DLLs locked.
# Stop only compiler-server processes; never terminate unrelated dotnet services.
Get-Process -Name VBCSCompiler -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host 'Cleaning previous Android build output...'
& dotnet clean $projectFile -f net10.0-android -c Debug -nodeReuse:false -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Android clean failed.' }

Write-Host 'Building a fresh signed Android APK...'
& dotnet build $projectFile -f net10.0-android -c Debug --no-incremental -nodeReuse:false -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'Android build failed.' }

if (-not (Test-Path -LiteralPath $apkPath)) {
    throw "The signed APK was not produced: $apkPath"
}

$apk = Get-Item -LiteralPath $apkPath
if ($apk.LastWriteTime -lt $buildStartedAt) {
    throw 'Refusing to deploy a stale APK from an earlier build.'
}

Write-Host "Fresh APK created: $($apk.FullName)"
if ($SkipInstall) { exit 0 }

$connectedDevices = @(& adb devices | Select-String -Pattern "\tdevice$")
if ($connectedDevices.Count -eq 0) {
    throw 'No authorized Android device is connected. The fresh APK was built but not installed.'
}

# The Android app uses 127.0.0.1:5232 in physical-device development.
# Forward that phone-local port over USB to the API running on this PC.
Write-Host 'Connecting the phone to the local API on port 5232...'
& adb reverse tcp:5232 tcp:5232
if ($LASTEXITCODE -ne 0) { throw 'Could not create the ADB reverse connection to the backend API.' }

Write-Host 'Installing and checking Android startup...'
& adb install -r $apkPath
if ($LASTEXITCODE -ne 0) { throw 'APK installation failed.' }

& adb logcat -c
& adb shell am force-stop $applicationId
& adb shell monkey -p $applicationId -c android.intent.category.LAUNCHER 1 | Out-Null
Start-Sleep -Seconds 10

$appProcessId = (& adb shell pidof $applicationId).Trim()
$fatalLog = & adb logcat -d -v brief |
    Select-String -Pattern 'FATAL EXCEPTION|XamlParseException|Resources\$NotFoundException'

if ([string]::IsNullOrWhiteSpace($appProcessId) -or $fatalLog) {
    if ($fatalLog) { $fatalLog | Write-Host }
    throw 'The app did not survive the startup verification. Review the fatal log above.'
}

Write-Host "Startup verified successfully. App PID: $appProcessId"
