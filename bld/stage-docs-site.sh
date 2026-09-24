#!/usr/bin/env bash
# ---------------------------------------------------------------------------------------------
# stage-docs-site.sh
#
# Lays a freshly built DocFX site into the directory layout the gh-pages branch serves, and does
# nothing else — no git, no network — so the layout can be exercised locally instead of only
# discovered in a deployment.
#
#   stage-docs-site.sh <site-dir> <work-dir> <slot>
#
#     site-dir   the DocFX output to publish (docs/_site)
#     work-dir   a checkout of the gh-pages branch, modified in place
#     slot       "dev", or a release series such as "1.0"
#
# Layout:
#
#   /            the latest release, so every URL published before versioning kept working
#   /<series>/   that release, archived (1.0, 1.1, ...) — written once, when its tag is pushed
#   /dev/        the current master build
#
# A release therefore lands twice, from one build: at the root and under its series. Keeping the
# root a full copy rather than a redirect is deliberate — a redirect only forwards the home page,
# and the links that matter are deep ones (api/Bodu.Core.html) that a reader or a README already
# holds. Those must not start 404ing the day versioning arrives.
#
# Writing a slot replaces that slot alone: publishing dev never touches a release, and publishing
# a release never touches dev or an earlier series.
# ---------------------------------------------------------------------------------------------
set -euo pipefail

site="${1:?usage: stage-docs-site.sh <site-dir> <work-dir> <slot>}"
work="${2:?usage: stage-docs-site.sh <site-dir> <work-dir> <slot>}"
slot="${3:?usage: stage-docs-site.sh <site-dir> <work-dir> <slot>}"

if [ ! -d "$site" ]; then
    printf '::error::site directory %s does not exist\n' "$site" >&2
    exit 1
fi
# An empty site would otherwise quietly replace a good one with nothing.
if [ ! -f "$site/index.html" ]; then
    printf '::error::%s has no index.html — refusing to publish what does not look like a built site\n' "$site" >&2
    exit 1
fi
if [ ! -d "$work" ]; then
    printf '::error::work directory %s does not exist\n' "$work" >&2
    exit 1
fi
if ! printf '%s' "$slot" | grep -qE '^(dev|[0-9]+\.[0-9]+)$'; then
    printf '::error::slot must be "dev" or a release series like "1.0", got %s\n' "$slot" >&2
    exit 1
fi

mkdir -p "$work"

copy_into() {
    # $1 = destination; replaced wholesale so a page deleted upstream does not linger.
    rm -rf "${1:?}"
    mkdir -p "$1"
    cp -R "$site"/. "$1"/
}

if [ "$slot" = "dev" ]; then
    copy_into "$work/dev"
else
    # Clear the root, keeping git's own directory, dev/, and every archived series. Without the
    # -prune the walk would descend into what it has just been told to keep.
    find "$work" -mindepth 1 -maxdepth 1 \
        \( -name '.git' -o -name 'dev' -o -regex '.*/[0-9][0-9]*\.[0-9][0-9]*' \) -prune \
        -o -exec rm -rf {} +
    cp -R "$site"/. "$work"/
    copy_into "$work/$slot"
fi

# GitHub Pages runs Jekyll over a branch source, and Jekyll drops directories whose names begin
# with an underscore — which is most of what DocFX emits. Without this file the styles and scripts
# 404 and the site renders unstyled.
touch "$work/.nojekyll"

printf 'Staged %s into %s (slot: %s)\n' "$site" "$work" "$slot"
