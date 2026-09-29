$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /out:"$PSScriptRoot\Flicker-2.5.exe" /win32icon:"$PSScriptRoot\assets\flicker.ico" "/resource:$PSScriptRoot\assets\flicker.ico,Flicker.Icon" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll "$PSScriptRoot\Clicker.cs" "$PSScriptRoot\Theme.cs" "$PSScriptRoot\Pause.cs" "$PSScriptRoot\PauseTests.cs" "$PSScriptRoot\MouseInput.cs" "$PSScriptRoot\KeyPicker.cs" "$PSScriptRoot\ScheduledPause.cs" "$PSScriptRoot\ScheduleTests.cs" "$PSScriptRoot\V2UI.cs"
if ($LASTEXITCODE -ne 0) { throw 'Kompilacja nie powiodła się.' }
Write-Output "Gotowe: $PSScriptRoot\Flicker-2.5.exe"
