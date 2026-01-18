Param(
    [string]$Source,
    [string]$Build,
    [string]$Install,
    [string]$Output
)

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

if (-not $Source) { $Source = Join-Path $root "vendor/thorvg" }
if (-not $Build) { $Build = Join-Path $root "artifacts/build/native" }
if (-not $Install) { $Install = Join-Path $root "artifacts/install/native" }

if (Test-Path $Install) { Remove-Item -Recurse -Force $Install }

$mesonArgs = @(
    "--buildtype=release",
    "--default-library=shared",
    "-Dengines=sw,gl",
    "-Dloaders=all",
    "-Dsavers=all",
    "-Dtools=",
    "-Dbindings=capi",
    "-Dpartial=true",
    "-Dfile=true",
    "-Dthreads=true",
    "-Dsimd=true",
    "-Dextra=lottie_exp,openmp",
    "-Dtests=false",
    "-Dlog=false"
)

function Invoke-Meson {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )
    & meson @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Meson failed with exit code $LASTEXITCODE."
    }
}

if (Test-Path $Build) {
    Invoke-Meson -Arguments (@("setup", "--wipe", $Build, $Source) + $mesonArgs)
} else {
    Invoke-Meson -Arguments (@("setup", $Build, $Source) + $mesonArgs)
}

Invoke-Meson -Arguments @("compile", "-C", $Build)
Invoke-Meson -Arguments @("install", "-C", $Build, "--destdir", $Install)

$nativeLib = $null
$searchRoots = @(
    (Join-Path $Build "src"),
    (Join-Path $Install "src")
) | Select-Object -Unique | Where-Object { Test-Path $_ }
$patterns = @("libthorvg*.dll", "thorvg*.dll", "libthorvg*.dylib", "libthorvg*.so", "libthorvg*.so.*")
foreach ($root in $searchRoots) {
    foreach ($pattern in $patterns) {
        $nativeLib = Get-ChildItem -Path $root -Recurse -File -Filter $pattern -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($nativeLib) { break }
    }
    if ($nativeLib) { break }
}

if (-not $nativeLib) {
    Write-Host "Failed to locate ThorVG native library. Searched roots: $($searchRoots -join ', ')"
    $dllCandidates = Get-ChildItem -Path $Build -Recurse -File -Include "libthorvg*.dll", "thorvg*.dll" -ErrorAction SilentlyContinue |
        Select-Object -First 10 -ExpandProperty FullName
    if ($dllCandidates) {
        Write-Host "Closest DLL candidates under build dir:"
        $dllCandidates | ForEach-Object { Write-Host "  $_" }
    }
    throw "Failed to locate ThorVG native library."
}

if ($Output) {
    if (-not (Test-Path $Output)) { New-Item -ItemType Directory -Path $Output | Out-Null }
    $targetName = switch -Regex ($nativeLib.Name) {
        '\.dll$' { "libthorvg.dll"; break }
        '\.dylib$' { "libthorvg.dylib"; break }
        '\.so(\..*)?$' { "libthorvg.so"; break }
        default { "libthorvg" }
    }
    Copy-Item -Path $nativeLib.FullName -Destination (Join-Path $Output $targetName) -Force
    $nativeLib = Join-Path $Output $targetName
} else {
    $nativeLib = $nativeLib.FullName
}

Write-Output $nativeLib
