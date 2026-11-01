#!/usr/bin/env bash
# ---------------------------------------------------------------------------------------------------------------
# collect-coverage.sh
#
# Collects code coverage for the Bodu solution, one test project at a time, into
# artifacts/coverage/raw/<TestProjectName>/**/coverage.cobertura.xml.
#
# Collection is PER PROJECT by design, not solution-wide:
#   * docs/articles/code-coverage.md prescribes per-project re-collection as the remedy for the
#     stale-path double-counting that a folder/namespace refactor leaves behind;
#   * it mirrors the per-project loop already in .github/workflows/build-test.yml, so one broken
#     assembly cannot poison the whole run; and
#   * one Cobertura file per project is the unit of sharding.
#
# The coverage tier, collector, and every exclusion live in coverage.runsettings - this script does
# NOT pass --collect, because a command-line collector is a second, unconfigured instance whose
# exclusion lists are empty.
#
# Collection is ONE target framework, every project the same one: the newest in BoduNetTargets
# (bld/TargetFrameworks.props), today net10.0, unless --framework names another. A test project
# targets every framework in BoduNetTargets, and an unrestricted `dotnet test` runs - and covers - each
# of them; keeping only one leg's file made the result depend on which leg finished last, and code under
# a framework conditional (#if NET10_0_OR_GREATER and its fallbacks) then reported different line sets
# from different projects, which the matrix's stale-numbering check reads as stale data.
#
# Usage:  bld/collect-coverage.sh [options]
#   --project <path>    Collect a single test project (repo-relative or absolute .csproj path).
#   --filter <regex>    Collect only test projects whose path matches the extended regex.
#   --shard <i/N>       Collect shard i of N (1-based), balanced by test-project size.
#   --configuration <c> Build configuration. Default: Release.
#   --framework <tfm>   Target framework to build, test, and cover. Default: the newest in
#                       BoduNetTargets, today net10.0.
#   --output <dir>      Artifact root. Default: artifacts/coverage.
#   --no-probe          Skip the AVX-512 capability probe.
#   --scalar            Re-run with hardware intrinsics disabled, into a parallel '<Name>.scalar'
#                       directory, so the merge unions the scalar and intrinsic paths.
#   --list              Print the selected test projects and exit without collecting.
#   -h | --help         Show this help.
#
# Emits <output>/report/environment.json describing the run - most importantly whether this host
# supports AVX-512, its architecture, and the names of the test projects it collected. The *.Avx512.cs
# and *.Vector512.cs files in Bodu.Security.Cryptography are hardware-gated: the JIT selects exactly one
# path per process, so on a host without AVX-512 those files CANNOT be covered and must be reported as
# 'n/a (hardware-gated)' rather than 0%. The ARM64 instruction-set shims (*.AdvSimd.cs, *.PmullIsa.cs)
# are gated the same way on any host that is not ARM64; the ARM64 job in .github/workflows/build-test.yml
# runs them. The matrix decides each package's gating from the manifest of the shard that collected
# that package's own test project, which is why the manifest names them (#1364). See
# docs/articles/code-coverage.md.
#
# Exit code: 0 when every selected project collected successfully, 1 otherwise.
# ---------------------------------------------------------------------------------------------------------------
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

configuration="Release"
framework=""
output_dir="artifacts/coverage"
single_project=""
filter=""
shard_spec=""
run_probe=true
list_only=false
scalar_pass=false

usage() {
    sed -n '2,51p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
}

while [ $# -gt 0 ]; do
    case "$1" in
        --project)       single_project="${2:?--project requires a path}"; shift 2 ;;
        --filter)        filter="${2:?--filter requires a regex}"; shift 2 ;;
        --shard)         shard_spec="${2:?--shard requires i/N}"; shift 2 ;;
        --configuration) configuration="${2:?--configuration requires a value}"; shift 2 ;;
        --framework)     framework="${2:?--framework requires a target framework}"; shift 2 ;;
        --output)        output_dir="${2:?--output requires a path}"; shift 2 ;;
        --no-probe)      run_probe=false; shift ;;
        --scalar)        scalar_pass=true; shift ;;
        --list)          list_only=true; shift ;;
        -h|--help)       usage; exit 0 ;;
        *) echo "error: unknown option '$1' (try --help)" >&2; exit 2 ;;
    esac
done

raw_dir="$output_dir/raw"
report_dir="$output_dir/report"

# The newest framework in BoduNetTargets, which lists them oldest first - so widening the matrix there moves
# the collection with it, and no framework is named here.
if [ -z "$framework" ]; then
    framework="$(sed -n 's:.*<BoduNetTargets>\(.*\)</BoduNetTargets>.*:\1:p' bld/TargetFrameworks.props \
        | head -1 | tr ';' '\n' | sed '/^[[:space:]]*$/d' | tail -1)"
    if [ -z "$framework" ]; then
        echo "error: could not read BoduNetTargets from bld/TargetFrameworks.props; pass --framework" >&2
        exit 2
    fi
fi

# ---------------------------------------------------------------------------------------------------------------
# Project discovery
#
# Derived from bodu.slnx rather than a glob, so the list stays in sync as projects are added or
# removed and the benchmark harnesses (not solution members) stay out. This is the same expression
# used by .github/workflows/build-test.yml.
# ---------------------------------------------------------------------------------------------------------------
discover_projects() {
    grep -oE 'Path="[^"]*\.Test\.csproj"' bodu.slnx \
        | sed -E 's/^Path="//; s/"$//; s/\\/\//g' \
        | sort -u
}

if [ -n "$single_project" ]; then
    # Normalise an absolute path back to repo-relative so artifact naming is stable.
    projects="${single_project#"$repo_root"/}"
    if [ ! -f "$projects" ]; then
        echo "error: test project not found: $projects" >&2
        exit 2
    fi
else
    projects="$(discover_projects)"
    if [ -z "$projects" ]; then
        echo "error: no test projects discovered in bodu.slnx" >&2
        exit 1
    fi
    if [ -n "$filter" ]; then
        projects="$(echo "$projects" | grep -E "$filter" || true)"
        if [ -z "$projects" ]; then
            echo "error: --filter '$filter' matched no test project" >&2
            exit 1
        fi
    fi
fi

# ---------------------------------------------------------------------------------------------------------------
# Weighted sharding
#
# Test-project sizes are wildly skewed (Bodu.Core ~678 test files against single-digit projects), so
# round-robin assignment produces a straggler shard several times longer than the rest. Greedy
# longest-processing-time bin-packing over the per-project .cs count balances wall clock instead, and
# because the weights are computed at run time it needs no maintenance as projects grow.
# ---------------------------------------------------------------------------------------------------------------
select_shard() {
    local index="$1" count="$2"

    while read -r proj; do
        [ -n "$proj" ] || continue
        printf '%s\t%s\n' "$(find "$(dirname "$proj")" -name '*.cs' 2>/dev/null | wc -l)" "$proj"
    done <<< "$projects" \
        | sort -rn \
        | awk -F'\t' -v idx="$index" -v n="$count" '
            BEGIN { for (i = 1; i <= n; i++) load[i] = 0 }
            {
                # Assign the next-largest project to the currently lightest shard.
                lightest = 1
                for (i = 2; i <= n; i++) if (load[i] < load[lightest]) lightest = i
                load[lightest] += $1
                if (lightest == idx) print $2
            }'
}

if [ -n "$shard_spec" ]; then
    if ! [[ "$shard_spec" =~ ^[0-9]+/[0-9]+$ ]]; then
        echo "error: --shard expects i/N (for example 2/8), got '$shard_spec'" >&2
        exit 2
    fi
    shard_index="${shard_spec%/*}"
    shard_count="${shard_spec#*/}"
    if [ "$shard_index" -lt 1 ] || [ "$shard_index" -gt "$shard_count" ]; then
        echo "error: --shard index $shard_index is outside 1..$shard_count" >&2
        exit 2
    fi
    projects="$(select_shard "$shard_index" "$shard_count")"
    if [ -z "$projects" ]; then
        echo "note: shard $shard_spec is empty (more shards than test projects); nothing to collect."
    fi
fi

if [ "$list_only" = true ]; then
    echo "$projects"
    exit 0
fi

mkdir -p "$raw_dir" "$report_dir"

# ---------------------------------------------------------------------------------------------------------------
# AVX-512 capability probe
#
# The authoritative answer is the runtime's, not /proc/cpuinfo's: the JIT can disable an ISA the CPU
# reports. The probe therefore compiles a throwaway console app OUTSIDE the repository, so the
# repo-wide Directory.Build.props/targets (analyzers, doc-comment warnings as errors) do not apply.
# ---------------------------------------------------------------------------------------------------------------
probe_avx512() {
    local dir
    dir="$(mktemp -d)" || { echo "unknown"; return; }

    cat > "$dir/probe.csproj" <<'CSPROJ'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
CSPROJ

    cat > "$dir/Program.cs" <<'CSHARP'
Console.WriteLine(System.Runtime.Intrinsics.X86.Avx512F.IsSupported ? "true" : "false");
CSHARP

    local result
    result="$(cd "$dir" && dotnet run --configuration Release --nologo 2>/dev/null | tail -1)"
    rm -rf "$dir"

    case "$result" in
        true|false) echo "$result" ;;
        *)          echo "unknown" ;;
    esac
}

# ---------------------------------------------------------------------------------------------------------------
# Collection
# ---------------------------------------------------------------------------------------------------------------
status=0
collected=0
collected_names=()

while read -r proj; do
    [ -n "$proj" ] || continue

    project_name="$(basename "$proj" .csproj)"
    name="$project_name"
    [ "$scalar_pass" = true ] && name="$name.scalar"
    echo "::group::Collect $name"

    # Clear any output from a previous run of this project. vstest writes each run into a fresh GUID
    # subdirectory, so without this the merge would union today's results with a stale report - and a
    # stale report collected before a rename carries source paths that no longer exist, which is
    # precisely the phantom-row double-counting documented in docs/articles/code-coverage.md.
    rm -rf "${raw_dir:?}/$name"

    if ! dotnet restore "$proj"; then
        echo "error: restore failed for $name" >&2
        status=1
        echo "::endgroup::"
        continue
    fi

    if ! dotnet build "$proj" --configuration "$configuration" --framework "$framework" --no-restore; then
        echo "error: build failed for $name" >&2
        status=1
        echo "::endgroup::"
        continue
    fi

    # The JIT selects exactly one of the SIMD or scalar implementations per process, so a single run
    # can never cover both. Disabling hardware intrinsics forces the scalar reference paths; merged
    # with a native run it yields the union. Measured on Bodu.Security.Cryptography, this moves
    # ThreefishBlockCipher.{256,512,1024}.cs from 19.0/12.8/8.3% to 90.5/93.1/94.9%.
    scalar_env=()
    if [ "$scalar_pass" = true ]; then
        # .NET 8 reads DOTNET_EnableAVX512F and .NET 10 DOTNET_EnableAVX512; each ignores the other's name.
        scalar_env=(DOTNET_EnableAVX512F=0 DOTNET_EnableAVX512=0 DOTNET_EnableAVX2=0 DOTNET_EnableHWIntrinsic=0)
        echo "Hardware intrinsics disabled for this pass."
    fi

    # No --collect here: the collector and every exclusion are configured in coverage.runsettings.
    if env ${scalar_env[@]+"${scalar_env[@]}"} dotnet test "$proj" \
        --configuration "$configuration" \
        --framework "$framework" \
        --no-build \
        --settings coverage.runsettings \
        --results-directory "$raw_dir/$name" \
        --logger "trx;LogFileName=$name.trx"
    then
        # The project ran once, for $framework, so every Cobertura file under its directory is that one
        # run's: the attachment in a GUID directory and, with some test platform versions, a byte-identical
        # copy in the data collector's staging directory (_<machine>_<timestamp>/In/<machine>/). Merging
        # both would double ReportGenerator's input, so normalise to exactly one file per project at a
        # predictable path. Files that differ mean more than one run reached the directory; choosing
        # among them by timestamp would make the result depend on which finished last, so that is an
        # error rather than a choice.
        reports="$(find "$raw_dir/$name" -name 'coverage.cobertura.xml' 2>/dev/null | sort)"
        distinct=0
        if [ -n "$reports" ]; then
            distinct="$(echo "$reports" | while read -r report; do sha256sum "$report" | cut -d' ' -f1; done | sort -u | wc -l)"
        fi
        first="$(echo "$reports" | head -1)"

        if [ "$distinct" -gt 1 ]; then
            echo "error: $distinct different coverage.cobertura.xml files produced for $name; expected one run for $framework" >&2
            echo "$reports" >&2
            status=1
        elif [ -n "$first" ]; then
            cp "$first" "$raw_dir/$name.cobertura.tmp"
            find "$raw_dir/$name" -mindepth 1 -type d -exec rm -rf {} + 2>/dev/null
            mv "$raw_dir/$name.cobertura.tmp" "$raw_dir/$name/coverage.cobertura.xml"
            collected=$((collected + 1))
            collected_names+=("$project_name")
        else
            echo "error: no coverage.cobertura.xml produced for $name" >&2
            status=1
        fi
    else
        echo "error: tests failed for $name" >&2
        status=1
    fi

    echo "::endgroup::"
done <<< "$projects"

# ---------------------------------------------------------------------------------------------------------------
# Run manifest
# ---------------------------------------------------------------------------------------------------------------
if [ -n "$shard_spec" ]; then
    manifest="$report_dir/environment.shard-${shard_spec//\//-of-}.json"
else
    manifest="$report_dir/environment.json"
fi

if [ "$scalar_pass" = true ]; then
    # A scalar pass is a supplementary run over a project the run of record already collected. Writing
    # a manifest here would either clobber that run's capability record or add a second, less
    # informative one for the matrix to reconcile.
    echo
    echo "Collected $collected scalar project(s) into $raw_dir"
    exit $status
fi

if [ "$run_probe" = true ]; then
    echo "Probing AVX-512 support..."
    avx512="$(probe_avx512)"
    echo "  Avx512F.IsSupported = $avx512"
else
    # A single-project top-up must not erase the capability record the full run established: the
    # matrix classifies the hardware-gated files from it, and losing it silently changes whether
    # those files are excluded from the denominator. Carry the previous answer forward.
    avx512="$(sed -n 's/.*"avx512Supported": "\([^"]*\)".*/\1/p' "$manifest" 2>/dev/null | head -1)"
    avx512="${avx512:-skipped}"

    if [ "$avx512" != "skipped" ]; then
        echo "AVX-512 probe skipped; carrying forward '$avx512' from $manifest"
    fi
fi

# The test projects this run collected, as a JSON array of their names, beside the count. The matrix
# reads them to find the shard that collected a package's own test project, because that shard's host
# is the only one whose capabilities say anything about the package's hardware-gated files: the hosted
# runners are a mixed fleet, and judging AVX-512 across every shard counted the cryptography kernels
# as uncovered whenever the crypto shard lacked it while another shard had it (#1364).
projects_json="[]"
if [ "$collected" -gt 0 ]; then
    projects_json="[$(printf '"%s", ' "${collected_names[@]}")]"
    projects_json="${projects_json%, ]}]"
fi

# Each shard runs in its own job and uploads its own artifact, which the aggregate job downloads into
# a single directory. A fixed filename would let the shards overwrite one another and lose the
# per-shard project lists, so the manifest name carries the shard identity (resolved above, before
# the probe reads the previous value back).
cat > "$manifest" <<JSON
{
  "commit": "$(git rev-parse HEAD 2>/dev/null || echo unknown)",
  "branch": "$(git rev-parse --abbrev-ref HEAD 2>/dev/null || echo unknown)",
  "configuration": "$configuration",
  "framework": "$framework",
  "shard": "${shard_spec:-full}",
  "projectsCollected": $collected,
  "projects": $projects_json,
  "avx512Supported": "$avx512",
  "os": "$(uname -s)",
  "arch": "$(uname -m)",
  "dotnetSdk": "$(dotnet --version 2>/dev/null || echo unknown)"
}
JSON

echo
echo "Collected $collected project(s) into $raw_dir"
echo "Run manifest: $manifest"

exit $status
