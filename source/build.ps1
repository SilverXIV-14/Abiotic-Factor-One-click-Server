$ErrorActionPreference = 'Stop'
$sourceRoot = $PSScriptRoot
$frameworkRoot = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$outputRoot = Join-Path $sourceRoot '..\build'
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
$references = @('System.dll','System.Core.dll','System.Xml.dll','System.Security.dll','System.Windows.Forms.dll','System.Drawing.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','System.Management.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll','System.Xaml.dll')
$compilerArgs = @('/nologo','/target:winexe','/platform:x64','/optimize+','/codepage:65001',('/out:' + (Join-Path $outputRoot 'GateControl.exe')),('/win32manifest:' + (Join-Path $sourceRoot 'app.manifest')),('/resource:' + (Join-Path $sourceRoot 'MainWindow.xaml') + ',MainWindow.xaml'),('/resource:' + (Join-Path $sourceRoot 'sandbox.tsv') + ',sandbox.tsv'))
if (Test-Path (Join-Path $sourceRoot 'app.ico')) { $compilerArgs += '/win32icon:' + (Join-Path $sourceRoot 'app.ico') }
foreach ($reference in $references) { $compilerArgs += '/reference:' + (Join-Path $frameworkRoot $reference) }
$compilerArgs += Get-ChildItem -LiteralPath $sourceRoot -Filter '*.cs' | ForEach-Object FullName
& (Join-Path $frameworkRoot 'csc.exe') @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Write-Output (Join-Path $outputRoot 'GateControl.exe')
