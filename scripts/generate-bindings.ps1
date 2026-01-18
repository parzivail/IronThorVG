Param(
    [string]$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$header = Join-Path $Root "vendor/thorvg/src/bindings/capi/thorvg_capi.h"
$outDir = Join-Path $Root "IronThorVG/Generated"
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

dotnet run --project (Join-Path $Root "tools/IronThorVG.Generator/IronThorVG.Generator.csproj") -- `
  --header "$header" `
  --output "$outDir"
