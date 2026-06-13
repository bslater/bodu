#!/usr/bin/env bash
# ---------------------------------------------------------------------------------------------------------------
# check-folder-namespace-alignment.sh
#
# Verifies the Bodu folder <-> namespace convention:
#   Folders directly under a project's src/ or test/ root are FLAT, dotted names equal to
#   (file namespace - <RootNamespace>). There are NO nested namespace folders, and a folder holds
#   only files whose namespace matches it. The file namespace is the source of truth.
#
# Carve-outs (not reported):
#   * Asset / embedded-resource folders: any path component named 'Fixtures' or 'TomlTestCorpus',
#     or ending in 'Resources' (calendar .xml packs etc.). These are not namespace-bearing.
#   * BCL-convention foreign namespaces: files declared in 'Microsoft.*' or 'System.*'
#     (e.g. DI registration extensions) live at the project root by design.
#   * Generated files: '*.Designer.cs'.
#
# Excludes the archive/ and docs/ trees. Scans the rest of the repo, including Bodu.CodeStyle.
#
# Usage:  bld/check-folder-namespace-alignment.sh [path-filter]
#   path-filter  optional substring; only csproj paths containing it are scanned.
#
# Exit code: 0 when aligned, 1 when one or more violations are found.
# ---------------------------------------------------------------------------------------------------------------
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
filter="${1:-}"
total=0

is_asset_path() {
    # true (0) when any path component is an exempt asset folder
    case "/$1/" in
        */Fixtures/*|*/TomlTestCorpus/*|*Resources/*) return 0 ;;
    esac
    return 1
}

scan_project() {
    local csproj="$1"
    local base proj rootns
    base="$(dirname "$csproj")"
    proj="$(basename "$csproj" .csproj)"
    rootns="$(grep -oP '(?<=<RootNamespace>)[^<]+' "$csproj" | head -1)"
    [ -z "$rootns" ] && rootns="$proj"

    local count=0 f rel ns exp act
    while IFS= read -r f; do
        case "$f" in */obj/*|*/bin/*) continue ;; esac
        rel="${f#"$base"/}"
        is_asset_path "$rel" && continue

        ns="$(grep -oP '^\s*namespace\s+\K[A-Za-z0-9_.]+' "$f" | head -1)"
        [ -z "$ns" ] && continue
        case "$ns" in Microsoft.*|System.*) continue ;; esac   # foreign-namespace carve-out

        if [ "$ns" = "$rootns" ]; then
            exp=""
        elif [ "${ns#"$rootns".}" != "$ns" ]; then
            exp="${ns#"$rootns".}"
        else
            exp="<<namespace '$ns' is not under RootNamespace '$rootns'>>"
        fi

        act="$(dirname "$rel")"
        [ "$act" = "." ] && act=""

        if [ "$act" != "$exp" ]; then
            count=$((count + 1))
            printf '    %s\n        namespace=%s  actual-folder=%s  expected-folder=%s\n' \
                "$rel" "$ns" "${act:-<root>}" "${exp:-<root>}"
        fi
    done < <(find "$base" -name '*.cs' -not -name '*.Designer.cs' 2>/dev/null)

    if [ "$count" -gt 0 ]; then
        echo "  [$proj] rootns=$rootns : $count violation(s)"
        total=$((total + count))
    fi
}

echo "Folder <-> namespace alignment check"
echo "===================================="
while IFS= read -r p; do
    [ -n "$filter" ] && [[ "$p" != *"$filter"* ]] && continue
    scan_project "$p"
done < <(find "$repo_root" -name '*.csproj' -not -path '*/archive/*' -not -path '*/docs/*' \
            -not -path '*/bin/*' -not -path '*/obj/*' | sort)

echo "------------------------------------"
if [ "$total" -eq 0 ]; then
    echo "OK: no violations."
    exit 0
fi
echo "FAIL: $total violation(s)."
exit 1
