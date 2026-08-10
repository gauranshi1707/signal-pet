param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$sdkRoot = Join-Path $env:LOCALAPPDATA 'Microsoft\dotnet'
$dotnet = Join-Path $sdkRoot 'dotnet.exe'
$windowsSdkBin = 'C:\Program Files (x86)\Windows Kits\10\bin\10.0.28000.0\x64'
$makePri = Join-Path $windowsSdkBin 'makepri.exe'
$makeAppx = Join-Path $windowsSdkBin 'makeappx.exe'
$signTool = Join-Path $windowsSdkBin 'signtool.exe'
$publishDirectory = Join-Path $projectRoot 'work\publish'
$stageDirectory = Join-Path $projectRoot 'work\msix-stage'
$packageDirectory = Join-Path $projectRoot 'outputs'
$packagePath = Join-Path $packageDirectory 'SignalPet-Development.msix'

foreach ($tool in @($dotnet, $makePri, $makeAppx, $signTool)) {
    if (-not (Test-Path $tool)) { throw "Required packaging tool was not found: $tool" }
}

Remove-Item -LiteralPath $publishDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $stageDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $packagePath -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDirectory, $stageDirectory, $packageDirectory | Out-Null

& $dotnet publish (Join-Path $projectRoot 'src\SignalPet\SignalPet.csproj') -c $Configuration -r win-x64 --self-contained true -o $publishDirectory
if ($LASTEXITCODE) { throw 'Publish failed.' }
Copy-Item -Path (Join-Path $publishDirectory '*') -Destination $stageDirectory -Recurse
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\SignalPet.Package\Package.appxmanifest') -Destination (Join-Path $stageDirectory 'AppxManifest.xml')

$assets = Join-Path $stageDirectory 'Assets'
New-Item -ItemType Directory -Force -Path $assets | Out-Null
Add-Type -AssemblyName System.Drawing
foreach ($asset in @(@{ Name = 'StoreLogo.png'; Size = 50 }, @{ Name = 'Square150x150Logo.png'; Size = 150 }, @{ Name = 'Square44x44Logo.png'; Size = 44 })) {
    $bitmap = [System.Drawing.Bitmap]::new($asset.Size, $asset.Size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::FromArgb(255, 123, 183, 255))
    $graphics.Dispose()
    $bitmap.Save((Join-Path $assets $asset.Name), [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
}

& $makePri createconfig /cf (Join-Path $stageDirectory 'priconfig.xml') /dq en-US
if ($LASTEXITCODE) { throw 'PRI configuration creation failed.' }
& $makePri new /pr $stageDirectory /cf (Join-Path $stageDirectory 'priconfig.xml') /of (Join-Path $stageDirectory 'resources.pri')
if ($LASTEXITCODE) { throw 'PRI generation failed.' }
Remove-Item -LiteralPath (Join-Path $stageDirectory 'priconfig.xml') -Force
& $makeAppx pack /d $stageDirectory /p $packagePath /o
if ($LASTEXITCODE) { throw 'MSIX package creation failed.' }

$certificate = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq 'CN=SignalPetDevelopment' -and $_.HasPrivateKey } | Select-Object -First 1
if ($null -eq $certificate) {
    $certificate = New-SelfSignedCertificate -Type Custom -Subject 'CN=SignalPetDevelopment' -KeyUsage DigitalSignature -CertStoreLocation 'Cert:\CurrentUser\My' -KeyExportPolicy Exportable
}
$certificatePath = Join-Path $stageDirectory 'SignalPetDevelopment.cer'
Export-Certificate -Cert $certificate -FilePath $certificatePath | Out-Null
Import-Certificate -FilePath $certificatePath -CertStoreLocation 'Cert:\CurrentUser\Root' | Out-Null
Remove-Item -LiteralPath $certificatePath -Force
& $signTool sign /fd SHA256 /sha1 $certificate.Thumbprint $packagePath
if ($LASTEXITCODE) { throw 'MSIX signing failed.' }
& $signTool verify /pa $packagePath
if ($LASTEXITCODE) { throw 'MSIX signature verification failed.' }
Add-AppxPackage -Path $packagePath -ForceApplicationShutdown

Write-Output "Installed development package: $packagePath"
