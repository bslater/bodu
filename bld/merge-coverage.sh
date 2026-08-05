#!/usr/bin/env bash
# ---------------------------------------------------------------------------------------------------------------
# merge-coverage.sh
#
# Merges the per-project Cobertura files produced by bld/collect-coverage.sh into a single report
# under artifacts/coverage/report/.
#
# ReportGenerator merges by (assembly, class, file) and takes the MAXIMUM hit count per line across
# inputs. That union semantic is what makes two things work:
#   * a source file exercised by more than one test assembly is credited once, not double-counted; and
#   * the AVX-512 dual run (Bodu.Security.Cryptography.Test on native hardware plus
#     Bodu.Security.Cryptography.Simd.Test with the SIMD feature switch off) yields a single figure
#     covering both the intrinsic and scalar paths. See docs/articles/code-coverage.md.
#
# Usage:  bld/merge-coverage.sh [options]
#   --input <dir>    Raw Cobertura root. Default: artifacts/coverage/raw.
#   --output <dir>   Report output. Default: artifacts/coverage/report.
#   -h | --help      Show this help.
#
# Exit code: 0 on success, 1 when no Cobertura input was found.
# ---------------------------------------------------------------------------------------------------------------
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

input_dir="artifacts/coverage/raw"
output_dir="artifacts/coverage/report"

usage() {
    sed -n '2,20p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'
}

while [ $# -gt 0 ]; do
    case "$1" in
        --input)   input_dir="${2:?--input requires a path}"; shift 2 ;;
        --output)  output_dir="${2:?--output requires a path}"; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) echo "error: unknown option '$1' (try --help)" >&2; exit 2 ;;
    esac
done

reports=$(find "$input_dir" -name 'coverage.cobertura.xml' 2>/dev/null | wc -l)
if [ "$reports" -eq 0 ]; then
    echo "error: no coverage.cobertura.xml found under '$input_dir'. Run bld/collect-coverage.sh first." >&2
    exit 1
fi

echo "Merging $reports Cobertura report(s) from $input_dir"

mkdir -p "$output_dir"

dotnet tool restore

# The file and assembly filters repeat the coverage.runsettings exclusions on purpose: a Cobertura
# file collected before those exclusions existed (or by a stray command-line collector) would
# otherwise readmit generated sources and the test-infrastructure assemblies to the denominator.
dotnet reportgenerator \
    -reports:"$input_dir/**/coverage.cobertura.xml" \
    -targetdir:"$output_dir" \
    -reporttypes:"Cobertura;JsonSummary;MarkdownSummaryGithub;HtmlInline_AzurePipelines" \
    -filefilters:"-**/*.Designer.cs;-**/CrcStandard.Catalog.cs" \
    -assemblyfilters:"+Bodu.*;-*.Test;-Bodu.Test;-Bodu.Financial.ExchangeRates.Testing" \
    -verbosity:Warning

echo
echo "Merged report written to $output_dir"
echo "  Cobertura : $output_dir/Cobertura.xml"
echo "  Summary   : $output_dir/Summary.json"
echo "  Markdown  : $output_dir/SummaryGithub.md"
echo "  HTML      : $output_dir/index.html"
