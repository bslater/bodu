#!/usr/bin/env bash
# ---------------------------------------------------------------------------------------------------------------
# check-release-manifest.sh
#
# Whole-tree gate on bld/release-manifest.txt and the release preconditions it implies. The manifest
# decides what reaches nuget.org and, through its first-shipped-version column, which packages get an
# ApiCompat baseline - so a malformed or incomplete entry is not a style problem, it is a bad release.
# A nuget.org version cannot be withdrawn afterwards, only delisted, which is why these are checked
# before a tag rather than discovered by a red release run after one.
#
# WHOLE-TREE, not diff-scoped (unlike check-policy.sh): the manifest is small, currently clean, and
# every line of it is load-bearing, so there is nothing to grandfather.
#
# Checks:
#   1. Format     - every entry is "<PackageId> <first-shipped-version>", version as MAJOR.MINOR.PATCH.
#   2. Unique     - no package id appears twice (a duplicate would publish twice and confuse the
#                   baseline lookup, which takes the first match).
#   3. Project    - every id resolves to a packable project (<Id>.csproj under a src/ directory).
#                   Note the id is NOT always the directory name: the regional calendar data packs
#                   live under Bodu.Globalization.Calendar.Data/, so this searches rather than assumes.
#   4. NotAhead   - no package claims a first-shipped version later than BoduBaseVersion. Such an entry
#                   would silently never receive a baseline, because the comparison in
#                   Directory.Build.targets would never be satisfied.
#   5. Readme     - every manifest package has a project-root README.md (bld/RELEASING.md
#                   precondition 4); it is packed as the NuGet readme and is a consumer's first page.
#   6. Tier       - that README carries an API-stability tier banner. A published package with no tier
#                   leaves a consumer unable to tell whether its surface is committed.
#   7. Icon       - bld/icons/<PackageId>.png exists. The docfx workflow checks the other direction
#                   (every .svg has a rasterized sibling); this checks that every SHIPPING package has
#                   an icon, which is the direction a release cares about.
#   8. Stream     - the package's tier agrees with the version stream it ships on. Bodu runs two:
#                   Stable-tier packages ship at BoduBaseVersion, and the Preview/Experimental tier
#                   ships at BoduPreviewVersion via <BoduPackageVersionOverride> in its own csproj.
#                   Nothing else reconciles those, so a package promoted to Stable but left on the
#                   preview stream (or a preview package with no override) would publish at the wrong
#                   version and only be noticed on nuget.org, where it cannot be taken back. A Stable
#                   package may also ship out of band, at a literal override ahead of BoduBaseVersion
#                   (bld/RELEASING.md); the check fails once BoduBaseVersion catches up with it, so the
#                   override cannot outlive the release it was made for.
#   9. Baseline   - each version stream has a package-validation baseline of its own, no later than the
#                   version that stream ships at: BoduPackageValidationBaseline for the Stable packages,
#                   BoduPreviewPackageValidationBaseline for the preview ones. The streams publish
#                   different versions, so a package validated against the other stream's baseline
#                   asks ApiCompat to restore a version it never published, and its pack fails with
#                   NU1102 before comparing any API.
#
# Withheld packages (named in the manifest's comment block) are deliberately absent and are not
# checked - they do not ship, so they owe consumers nothing.
#
# Usage:  bld/check-release-manifest.sh
# Exit code: 0 when the manifest is clean, 1 when one or more violations are found.
# ---------------------------------------------------------------------------------------------------------------
set -uo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
manifest="$repo_root/bld/release-manifest.txt"
versioning="$repo_root/bld/Versioning.props"

violations=0

# Emits a GitHub Actions error annotation plus a human-readable line. The annotation points at
# bld/release-manifest.txt unless a second argument names another file.
fail() {
    local message="$1" file="${2:-bld/release-manifest.txt}"
    printf '::error file=%s::%s\n' "$file" "$message"
    printf '  VIOLATION: %s\n' "$message"
    violations=$((violations + 1))
}

# Emits a GitHub Actions notice annotation plus a human-readable line, for a deliberate state worth
# seeing in every run that is not a violation.
note() {
    local message="$1"
    printf '::notice file=bld/release-manifest.txt::%s\n' "$message"
    printf '  NOTE: %s\n' "$message"
}

if [ ! -f "$manifest" ]; then
    printf '::error::bld/release-manifest.txt not found\n'
    exit 1
fi

base_version="$(sed -n 's:.*<BoduBaseVersion>\(.*\)</BoduBaseVersion>.*:\1:p' "$versioning" | head -1)"
if [ -z "$base_version" ]; then
    printf '::error file=bld/Versioning.props::BoduBaseVersion could not be read\n'
    exit 1
fi

preview_version="$(sed -n 's:.*<BoduPreviewVersion>\(.*\)</BoduPreviewVersion>.*:\1:p' "$versioning" | head -1)"
if [ -z "$preview_version" ]; then
    printf '::error file=bld/Versioning.props::BoduPreviewVersion could not be read\n'
    exit 1
fi

stable_baseline="$(sed -n 's:.*<BoduPackageValidationBaseline>\(.*\)</BoduPackageValidationBaseline>.*:\1:p' "$versioning" | head -1)"
preview_baseline="$(sed -n 's:.*<BoduPreviewPackageValidationBaseline>\(.*\)</BoduPreviewPackageValidationBaseline>.*:\1:p' "$versioning" | head -1)"

printf 'Release manifest check\n'
printf '======================\n'
printf 'BoduBaseVersion:    %s  (Stable tier, validated against %s)\n' "$base_version" "${stable_baseline:-no baseline}"
printf 'BoduPreviewVersion: %s  (Preview / Experimental tier, validated against %s)\n' "$preview_version" "${preview_baseline:-no baseline of its own}"

# Compares two MAJOR.MINOR.PATCH versions; prints 1 when $1 > $2, else 0.
version_gt() {
    local a="$1" b="$2"
    [ "$a" = "$b" ] && { printf '0'; return; }
    if [ "$(printf '%s\n%s\n' "$a" "$b" | sort -V | head -1)" = "$a" ]; then printf '0'; else printf '1'; fi
}

# Checks a package's README tier banner and that its version stream agrees with that tier
# (manifest checks 5, 6 and 8). Shared by the manifest loop and the withheld sweep below: a
# withheld package packs exactly like a shipping one, so the day it is released it must already
# carry the right tier and the right stream. Reads and increments the caller's $violations
# through fail(), so it must not be run in a subshell.
check_tier_and_stream() {
    local id="$1" project_root="$2" project="$3"
    local readme tier override

    readme="$project_root/README.md"
    tier=""
    if [ ! -f "$readme" ]; then
        fail "$id: no README.md at $(realpath --relative-to="$repo_root" "$project_root") (bld/RELEASING.md precondition 4)"
    else
        tier="$(head -c 2000 "$readme" \
            | grep -oE 'API stability[[:space:]]*-+[[:space:]]*\*{0,2}(Stable|Preview|Experimental)' \
            | grep -oE '(Stable|Preview|Experimental)' | head -1)"
        if [ -z "$tier" ]; then
            fail "$id: README.md carries no API-stability tier banner (Stable / Preview / Experimental)"
        fi
    fi

    override="$(sed -n 's:.*<BoduPackageVersionOverride>\(.*\)</BoduPackageVersionOverride>.*:\1:p' "$project" | head -1)"
    case "$tier" in
        Stable)
            if [ "$override" = '$(BoduPreviewVersion)' ]; then
                fail "$id: tiered Stable but its csproj sets <BoduPackageVersionOverride>\$(BoduPreviewVersion)</BoduPackageVersionOverride>, so it would ship on the preview stream ($preview_version) instead of BoduBaseVersion $base_version. Remove the override when promoting a package to Stable, and until its first Stable release is on nuget.org pin its PackageValidationBaselineVersion to its last preview release (bld/Versioning.props)."
            elif [ -n "$override" ]; then
                # An out-of-band release: a Stable package may ship ahead of the lock-step version, and
                # only ahead of it. Once BoduBaseVersion reaches the override the package must rejoin
                # the stream, and the stream must pass a version this package has already published.
                if ! printf '%s' "$override" | grep -qE '^[0-9]+\.[0-9]+\.[0-9]+$'; then
                    fail "$id: tiered Stable but its csproj sets <BoduPackageVersionOverride>$override</BoduPackageVersionOverride>. A Stable package leaves the lock-step stream only for an out-of-band release, at a literal MAJOR.MINOR.PATCH version ahead of BoduBaseVersion $base_version (bld/RELEASING.md)."
                elif [ "$(version_gt "$override" "$base_version")" != 1 ]; then
                    fail "$id: ships out of band at $override, but BoduBaseVersion $base_version has caught up with it. Remove the override (and any pinned PackageValidationBaselineVersion) so the package rejoins the lock-step stream, and keep BoduBaseVersion above $override: this package has already published $override, and nuget.org cannot take it back."
                else
                    note "$id ships out of band at $override, ahead of BoduBaseVersion $base_version. Remove its override once BoduBaseVersion moves past $override."
                fi
            fi
            ;;
        Preview|Experimental)
            if [ -z "$override" ]; then
                fail "$id: tiered $tier but sets no <BoduPackageVersionOverride>, so it would ship at BoduBaseVersion $base_version alongside the Stable packages. Add <BoduPackageVersionOverride>\$(BoduPreviewVersion)</BoduPackageVersionOverride> to its csproj."
            elif [ "$override" != '$(BoduPreviewVersion)' ]; then
                # A literal is rejected even when it currently equals BoduPreviewVersion: it agrees by
                # coincidence, and stops agreeing silently the next time the preview stream moves.
                fail "$id: tiered $tier but pins its version override to the literal '$override' rather than \$(BoduPreviewVersion) (currently $preview_version). Reference the property so the preview stream moves in one edit and cannot drift."
            fi
            ;;
    esac
}

entries=0
preview_entries=0
seen_ids=" "

while IFS= read -r raw; do
    line="${raw%%#*}"
    line="$(printf '%s' "$line" | tr -d '\r')"
    [ -z "$(printf '%s' "$line" | tr -d '[:space:]')" ] && continue

    entries=$((entries + 1))
    read -r id version extra <<<"$line"

    # 1. Format.
    if [ -n "${extra:-}" ]; then
        fail "$id: unexpected trailing text '$extra' (expected '<PackageId> <first-shipped-version>')"
        continue
    fi
    if [ -z "${version:-}" ]; then
        fail "$id: missing first-shipped version (expected '<PackageId> <first-shipped-version>')"
        continue
    fi
    if ! printf '%s' "$version" | grep -qE '^[0-9]+\.[0-9]+\.[0-9]+$'; then
        fail "$id: first-shipped version '$version' is not MAJOR.MINOR.PATCH"
        continue
    fi

    # 2. Unique.
    case "$seen_ids" in
        *" $id "*) fail "$id: listed more than once" ;;
        *) seen_ids="$seen_ids$id " ;;
    esac

    # 3. Project - resolved by search, because the id is not always the directory name.
    project="$(find "$repo_root" -name "$id.csproj" -path '*/src/*' -not -path '*/obj/*' -not -path '*/bin/*' 2>/dev/null | head -1)"
    if [ -z "$project" ]; then
        fail "$id: no <Id>.csproj found under any src/ directory - is the id spelled correctly?"
        continue
    fi
    project_root="$(dirname "$(dirname "$project")")"

    # Counted for check 9, which needs to know whether the preview stream ships anything.
    if [ "$(sed -n 's:.*<BoduPackageVersionOverride>\(.*\)</BoduPackageVersionOverride>.*:\1:p' "$project" | head -1)" = '$(BoduPreviewVersion)' ]; then
        preview_entries=$((preview_entries + 1))
    fi

    # 4. NotAhead.
    if [ "$(version_gt "$version" "$base_version")" = "1" ]; then
        fail "$id: first-shipped version $version is ahead of BoduBaseVersion $base_version - it would never receive a package-validation baseline"
    fi

    # 5/6/8. README, its tier banner, and the version stream that tier implies.
    check_tier_and_stream "$id" "$project_root" "$project"

    # 7. Icon.
    if [ ! -f "$repo_root/bld/icons/$id.png" ]; then
        fail "$id: no bld/icons/$id.png (packed as the NuGet icon)"
    fi
done < "$manifest"

# The withheld sweep.
#
# The manifest also records the packages deliberately kept off nuget.org, as comment lines that are
# nothing but a package id followed by the reasoning. Those packages pack with everything else, so
# they can drift from their declared tier exactly as a shipping one can - and nothing noticed,
# because every check above iterates manifest DATA lines only. The day one of them is released is
# the day nobody re-reads its csproj, so it is checked now instead.
#
# Only the tier and stream are checked. A first-shipped version and a NuGet icon are release
# artifacts a withheld package has no reason to carry yet.
#
# The grammar is the one .github/workflows/release.yml and bld/check-docs.py already parse: inside
# the comment block, a line that is only a Bodu package id opens an entry.
withheld=0
while IFS= read -r raw; do
    case "$raw" in
        '#'*) ;;
        *) continue ;;
    esac

    stripped="$(printf '%s' "$raw" | tr -d '\r' | sed 's/^[[:space:]]*#[[:space:]]*//; s/[[:space:]]*$//')"
    printf '%s' "$stripped" | grep -qE '^Bodu\.[A-Za-z0-9.]+$' || continue

    withheld=$((withheld + 1))
    id="$stripped"

    project="$(find "$repo_root" -name "$id.csproj" -path '*/src/*' -not -path '*/obj/*' -not -path '*/bin/*' 2>/dev/null | head -1)"
    if [ -z "$project" ]; then
        fail "$id: recorded as withheld but no $id.csproj exists under any src/ directory - is the id spelled correctly?"
        continue
    fi

    check_tier_and_stream "$id" "$(dirname "$(dirname "$project")")" "$project"
done < "$manifest"

# 9. Baseline.
#
# A pack compares a manifest package with the baseline version of its own stream (selected in
# Directory.Build.targets), and ApiCompat first restores that version from nuget.org. A baseline
# ahead of its stream's version names a release that cannot have happened yet, and a preview package
# compared with the Stable baseline names a version it never shipped at: either way the pack fails
# with NU1102. Whether a baseline at or below its stream's version is actually on nuget.org takes a
# network lookup, which this offline check leaves to the pack itself.
check_baseline() {
    local property="$1" value="$2" stream_property="$3" stream_version="$4"

    if ! printf '%s' "$value" | grep -qE '^[0-9]+\.[0-9]+\.[0-9]+$'; then
        fail "$property '$value' is not MAJOR.MINOR.PATCH" bld/Versioning.props
    elif [ "$(version_gt "$value" "$stream_version")" = 1 ]; then
        fail "$property $value is ahead of $stream_property $stream_version, so it names a release this stream cannot have published yet, and every pack it applies to fails with NU1102. Set it to the last $stream_property already on nuget.org (bld/RELEASING.md, Post-publish)." bld/Versioning.props
    fi
}

if [ -n "$stable_baseline" ]; then
    check_baseline BoduPackageValidationBaseline "$stable_baseline" BoduBaseVersion "$base_version"
fi

if [ "$preview_entries" -gt 0 ]; then
    if [ -z "$preview_baseline" ]; then
        fail "$preview_entries manifest package(s) ship on the preview stream, but bld/Versioning.props sets no BoduPreviewPackageValidationBaseline, so they are compared with BoduPackageValidationBaseline (${stable_baseline:-unset}), the Stable stream's baseline. Once that moves to a Stable release, which no preview package shipped at, every preview pack fails with NU1102. Give the preview stream its own baseline: the last BoduPreviewVersion on nuget.org." bld/Versioning.props
    else
        check_baseline BoduPreviewPackageValidationBaseline "$preview_baseline" BoduPreviewVersion "$preview_version"
    fi
fi

printf -- '----------------------\n'
printf 'Manifest entries checked: %d\n' "$entries"
printf 'Withheld packages checked: %d\n' "$withheld"

if [ "$entries" -eq 0 ]; then
    printf '::error file=bld/release-manifest.txt::The manifest has no entries - a release would publish nothing\n'
    exit 1
fi

if [ "$violations" -gt 0 ]; then
    printf 'FAILED: %d violation(s).\n' "$violations"
    exit 1
fi

printf 'OK: no violations.\n'
