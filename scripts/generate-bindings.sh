#!/usr/bin/env bash
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
header="${root_dir}/vendor/thorvg/src/bindings/capi/thorvg_capi.h"
out_dir="${root_dir}/IronThorVG/Generated"

mkdir -p "${out_dir}"

dotnet run --project "${root_dir}/tools/IronThorVG.Generator/IronThorVG.Generator.csproj" -- \
  --header "${header}" \
  --output "${out_dir}"
