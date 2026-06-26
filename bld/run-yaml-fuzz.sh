#!/usr/bin/env bash
#
# Fuzz the Bodu YAML parser with SharpFuzz (libFuzzer for .NET), seeded from the yaml-test-suite corpus.
#
# Prerequisites (present in the Claude Code remote environment): clang, the .NET SDK, and network access to
# NuGet (SharpFuzz.CommandLine) and GitHub (the libfuzzer-dotnet driver source). The yaml-test-suite git
# submodule must be initialized (the session-start hook handles this).
#
# Usage:
#   bld/run-yaml-fuzz.sh                 # set up and fuzz for $BUDGET seconds (default 1500 ~= 25 min)
#   BUDGET=60 bld/run-yaml-fuzz.sh       # short smoke run
#   FUZZ_TARGET=roundtrip bld/run-yaml-fuzz.sh   # isolate one target (parse|stream|deser|reader|roundtrip)
#   bld/run-yaml-fuzz.sh setup           # only build the driver, publish, and instrument
#   bld/run-yaml-fuzz.sh triage <file>   # replay one input and print the managed exception/stack
#   bld/run-yaml-fuzz.sh minimize <file> # shrink a crashing input to its smallest form
#
# All artifacts live under Bodu.Text.Yaml/fuzz/.work (git-ignored). Crashing inputs land in .work/findings.

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
fuzz_proj="$repo_root/Bodu.Text.Yaml/fuzz/Bodu.Text.Yaml.Fuzz.csproj"
suite="$repo_root/Bodu.Text.Yaml/test/yaml-test-suite"

work="$repo_root/Bodu.Text.Yaml/fuzz/.work"
bin="$work/bin"
seed="$work/seed"
corpus="$work/corpus"
findings="$work/findings"
tooldir="$work/tools"
driver="$work/libfuzzer-dotnet"

budget="${BUDGET:-1500}"

mkdir -p "$work" "$corpus" "$findings" "$tooldir"

log() { printf '[run-yaml-fuzz] %s\n' "$*"; }

# The pinned libfuzzer-dotnet release providing the prebuilt Ubuntu driver and the driver source.
driver_release="${LIBFUZZER_DOTNET_RELEASE:-v2025.05.02.0904}"

ensure_driver() {
    if [ -x "$driver" ]; then return 0; fi

    # Prefer the prebuilt Ubuntu driver (statically linked with libFuzzer); it avoids needing the clang
    # fuzzer runtime archives, which are not always installed.
    log "Downloading the prebuilt libfuzzer-dotnet driver ($driver_release)..."
    if curl -fsSL --max-time 180 \
        "https://github.com/Metalnem/libfuzzer-dotnet/releases/download/$driver_release/libfuzzer-dotnet-ubuntu" \
        -o "$driver" && [ -s "$driver" ]; then
        chmod +x "$driver"
        log "Driver downloaded: $driver"
        return 0
    fi

    # Fallback: compile from source (requires the clang libFuzzer runtime).
    log "Prebuilt download failed; compiling the driver from source with clang..."
    local td cc
    td="$(mktemp -d)"
    curl -fsSL --max-time 180 "https://codeload.github.com/Metalnem/libfuzzer-dotnet/tar.gz/refs/heads/master" -o "$td/libfuzzer-dotnet.tgz"
    tar -xzf "$td/libfuzzer-dotnet.tgz" -C "$td"
    cc="$(find "$td" -name 'libfuzzer-dotnet.cc' | head -1)"
    [ -n "$cc" ] || { log "ERROR: libfuzzer-dotnet.cc not found."; exit 1; }
    clang -g -O2 -fsanitize=fuzzer "$cc" -o "$driver"
    rm -rf "$td"
    log "Driver built: $driver"
}

ensure_tool() {
    if [ -x "$tooldir/sharpfuzz" ]; then return 0; fi
    log "Installing the SharpFuzz.CommandLine instrumentation tool..."
    dotnet tool install --tool-path "$tooldir" SharpFuzz.CommandLine >/dev/null
}

publish_and_instrument() {
    log "Publishing the fuzz harness (Release)..."
    rm -rf "$bin"
    dotnet publish "$fuzz_proj" -c Release -o "$bin" >/dev/null
    log "Instrumenting Bodu.Text.Yaml.dll for coverage..."
    "$tooldir/sharpfuzz" "$bin/Bodu.Text.Yaml.dll" >/dev/null
}

build_seed() {
    [ -f "$suite/229Q/in.yaml" ] || { log "ERROR: yaml-test-suite submodule is not populated ($suite)."; exit 1; }
    log "Building the seed corpus from yaml-test-suite in.yaml vectors..."
    rm -rf "$seed"; mkdir -p "$seed"
    while IFS= read -r f; do
        local id
        id="$(dirname "${f#"$suite"/}" | tr '/' '-')"
        cp "$f" "$seed/$id.yaml"
    done < <(find "$suite" -name 'in.yaml' -not -path '*/name/*' -not -path '*/tags/*')
    log "Seed corpus: $(find "$seed" -type f | wc -l) inputs."
}

ensure_setup() {
    ensure_tool
    ensure_driver
    publish_and_instrument
    build_seed
}

target_exe="$bin/Bodu.Text.Yaml.Fuzz"

mode="${1:-run}"
case "$mode" in
    setup)
        ensure_setup
        log "Setup complete."
        ;;
    run)
        ensure_setup
        # FORK=1 keeps fuzzing past crashes (each input runs in a subprocess), collecting every distinct
        # finding in one run instead of stopping at the first.
        fork_args=()
        [ "${FORK:-0}" = "1" ] && fork_args=(-fork=1 -ignore_crashes=1)
        log "Fuzzing for ${budget}s (FUZZ_TARGET=${FUZZ_TARGET:-all}${FORK:+, fork}); crashes land in $findings ..."
        cd "$findings"
        # The first positional corpus dir accumulates interesting inputs; the seed dir is read-only seeds.
        "$driver" --target_path="$target_exe" "$corpus" "$seed" \
            -timeout=10 -rss_limit_mb=2048 -max_total_time="$budget" -print_final_stats=1 "${fork_args[@]}" \
            || log "libFuzzer exited non-zero (a crash was found, or the run was stopped)."
        log "Findings in $findings:"
        find "$findings" -maxdepth 1 -type f \( -name 'crash-*' -o -name 'oom-*' -o -name 'timeout-*' -o -name 'leak-*' \) -printf '  %f\n' || true
        ;;
    triage)
        f="${2:?usage: run-yaml-fuzz.sh triage <file>}"
        [ -x "$target_exe" ] || ensure_setup
        log "Replaying $f (FUZZ_TARGET=${FUZZ_TARGET:-all}) ..."
        "$driver" --target_path="$target_exe" "$f"
        ;;
    minimize)
        f="${2:?usage: run-yaml-fuzz.sh minimize <file>}"
        [ -x "$target_exe" ] || ensure_setup
        cd "$findings"
        "$driver" --target_path="$target_exe" -minimize_crash=1 -runs=200000 "$f"
        ;;
    *)
        log "Unknown mode '$mode'. See the header for usage."
        exit 2
        ;;
esac
