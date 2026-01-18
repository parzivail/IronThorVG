#!/usr/bin/env bash
set -euo pipefail

root_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

source_dir=""
build_dir=""
install_dir=""
output_dir=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --source)
      source_dir="$2"
      shift 2
      ;;
    --build)
      build_dir="$2"
      shift 2
      ;;
    --install)
      install_dir="$2"
      shift 2
      ;;
    --output)
      output_dir="$2"
      shift 2
      ;;
    *)
      echo "Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

source_dir="${source_dir:-${root_dir}/vendor/thorvg}"
build_dir="${build_dir:-${root_dir}/artifacts/build/native}"
install_dir="${install_dir:-${root_dir}/artifacts/install/native}"

rm -rf "${install_dir}"

run_cmd() {
  "$@" 1>&2
}

if [[ -d "${build_dir}" ]]; then
  run_cmd meson setup --wipe "${build_dir}" "${source_dir}" \
    --buildtype=release \
    --default-library=shared \
    -Dengines=sw,gl \
    -Dloaders=all \
    -Dsavers=all \
    -Dtools= \
    -Dbindings=capi \
    -Dpartial=true \
    -Dfile=true \
    -Dthreads=true \
    -Dsimd=true \
    -Dextra=lottie_exp,openmp \
    -Dtests=false \
    -Dlog=false
else
  run_cmd meson setup "${build_dir}" "${source_dir}" \
    --buildtype=release \
    --default-library=shared \
    -Dengines=sw,gl \
    -Dloaders=all \
    -Dsavers=all \
    -Dtools= \
    -Dbindings=capi \
    -Dpartial=true \
    -Dfile=true \
    -Dthreads=true \
    -Dsimd=true \
    -Dextra=lottie_exp,openmp \
    -Dtests=false \
    -Dlog=false
fi

run_cmd meson compile -C "${build_dir}"
run_cmd meson install -C "${build_dir}" --destdir "${install_dir}"

native_lib=""
search_roots=()
for root in "${build_dir}/src" "${install_dir}/src"; do
  if [[ -d "${root}" ]]; then
    search_roots+=("${root}")
  fi
done

for root in "${search_roots[@]}"; do
  for pattern in "libthorvg*.so" "libthorvg*.so.*" "libthorvg*.dylib" "libthorvg*.dll" "thorvg*.dll"; do
    native_lib=$(find "${root}" -type f -name "${pattern}" | sort | head -n 1 || true)
    if [[ -n "${native_lib}" ]]; then
      break
    fi
  done
  if [[ -n "${native_lib}" ]]; then
    break
  fi
done

if [[ -z "${native_lib}" ]]; then
  echo "Failed to locate ThorVG native library. Searched roots: ${search_roots[*]:-(none)}" >&2
  exit 1
fi

if [[ -n "${output_dir}" ]]; then
  mkdir -p "${output_dir}"
  case "${native_lib}" in
    *.dll) target_name="libthorvg.dll" ;;
    *.dylib) target_name="libthorvg.dylib" ;;
    *.so*) target_name="libthorvg.so" ;;
    *) target_name="libthorvg" ;;
  esac
  cp "${native_lib}" "${output_dir}/${target_name}"
  native_lib="${output_dir}/${target_name}"
fi

echo "${native_lib}"
