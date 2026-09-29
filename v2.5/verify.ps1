$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$output=Join-Path $PSScriptRoot 'obj'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$sources=@('Clicker.cs','Theme.cs','Pause.cs','PauseTests.cs','MouseInput.cs','KeyPicker.cs','ScheduledPause.cs','ScheduleTests.cs','V2UI.cs') | ForEach-Object { Join-Path $PSScriptRoot $_ }
& $compiler /nologo /target:exe "/out:$output\Flicker.Tests.exe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll $sources
if($LASTEXITCODE -ne 0) { throw 'Kompilacja testów nie powiodła się.' }
& "$output\Flicker.Tests.exe" --self-test
if($LASTEXITCODE -ne 0) { throw 'Testy nie powiodły się.' }
& $compiler /nologo /target:exe /main:Preview "/out:$output\Flicker.Preview.exe" "/resource:$PSScriptRoot\assets\flicker.ico,Flicker.Icon" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll $sources "$PSScriptRoot\Preview.cs"
if($LASTEXITCODE -ne 0) { throw 'Kompilacja podglądu nie powiodła się.' }
Push-Location $output
try { & "$output\Flicker.Preview.exe" } finally { Pop-Location }
