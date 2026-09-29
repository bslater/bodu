#!/bin/bash
# Installs the .NET SDK that global.json pins, and the .NET 8 runtime, on
# session start so the agent can run `dotnet build` and `dotnet test` against
# bodu.slnx, and repairs the dotnet-dnceng Claude Code plugin whose upstream
# manifest currently fails validation (see repair_dnceng_plugin below). Only
# runs in the remote Claude Code on the web environment; on a developer's local
# machine the SDK is expected to be installed already.
#
# SDK 10.0 is required because the solution uses C# 14 language features (for
# example the `field` keyword on semi-auto properties). An older SDK such as
# 8.0 cannot compile the sources. global.json pins the exact 10.0 SDK that CI
# builds with, so the agent sees the same analyzer warnings CI does.
#
# The script is idempotent: when the pinned SDK and a .NET 8 runtime are
# already installed it installs nothing, so re-invocation (resume, clear,
# compact) is essentially free.
set -euo pipefail

# Only act in the remote environment; local sessions are left untouched.
if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
    exit 0
fi

# ---------------------------------------------------------------------------
# dotnet-dnceng plugin repair
#
# .claude/settings.json registers the dotnet/arcade-skills marketplace and
# enables its dotnet-dnceng plugin. The upstream plugin manifest currently
# declares "agents": ["agents/ci-investigator.agent.md"], but Claude Code
# requires plugin-relative paths to start with "./", so the automatic install
# at session start fails manifest validation. Patch the cached marketplace
# clone and install the plugin so its skills load from the next session in
# this container onward. Idempotent, and a no-op once the manifest is fixed
# upstream; failures never block session start.
# ---------------------------------------------------------------------------
repair_dnceng_plugin() {
    command -v claude >/dev/null 2>&1 || return 0

    local manifest="$HOME/.claude/plugins/marketplaces/dotnet-arcade-skills/plugins/dotnet-dnceng/plugin.json"
    [ -f "$manifest" ] || return 0

    if grep -q '"agents/ci-investigator\.agent\.md"' "$manifest"; then
        sed -i 's#"agents/ci-investigator\.agent\.md"#"./agents/ci-investigator.agent.md"#' "$manifest"
        echo "[session-start] Patched dotnet-dnceng plugin manifest (agents path lacked ./ prefix)."
    fi

    if [ ! -d "$HOME/.claude/plugins/cache/dotnet-arcade-skills/dotnet-dnceng" ]; then
        if claude plugin install dotnet-dnceng@dotnet-arcade-skills >/dev/null 2>&1; then
            echo "[session-start] Installed dotnet-dnceng plugin; its skills load from the next session."
        else
            echo "[session-start] dotnet-dnceng plugin install failed; its skills are unavailable in this container." >&2
        fi
    fi
}
repair_dnceng_plugin || true

# ---------------------------------------------------------------------------
# yaml-test-suite submodule
#
# Bodu.Text.Yaml links the yaml/yaml-test-suite conformance corpus as the
# 'yaml-test-suite' git submodule. The Regression test tier needs its vectors
# present in the working tree. A normal `git submodule update --init` works
# wherever GitHub is reachable, but the remote Claude Code on the web git proxy
# only serves the in-scope repository and returns 403 for external clones, so
# fall back to fetching the pinned release tarball over the HTTPS proxy and
# populating the submodule working tree. Idempotent and never blocks startup.
# ---------------------------------------------------------------------------
ensure_yaml_test_suite() {
    local repo_root sub_path ref
    repo_root="$(git -C "$(dirname "${BASH_SOURCE[0]}")" rev-parse --show-toplevel 2>/dev/null)" || return 0
    sub_path="$repo_root/Bodu.Text.Yaml/test/yaml-test-suite"
    ref="data-2022-01-17"

    # Already populated.
    [ -f "$sub_path/229Q/in.yaml" ] && return 0

    # Preferred path: a real submodule checkout where the upstream is reachable.
    if git -C "$repo_root" submodule update --init Bodu.Text.Yaml/test/yaml-test-suite >/dev/null 2>&1 \
        && [ -f "$sub_path/229Q/in.yaml" ]; then
        echo "[session-start] Initialized yaml-test-suite submodule."
        return 0
    fi

    # Fallback: fetch the pinned release tarball over HTTPS and populate the tree.
    local tmp src
    tmp="$(mktemp -d)"
    if curl -fsSL --max-time 120 "https://codeload.github.com/yaml/yaml-test-suite/tar.gz/refs/tags/$ref" -o "$tmp/corpus.tgz" \
        && tar -xzf "$tmp/corpus.tgz" -C "$tmp"; then
        src="$(find "$tmp" -maxdepth 1 -type d -name 'yaml-test-suite-*' | head -1)"
        if [ -n "$src" ]; then
            mkdir -p "$sub_path"
            cp -a "$src"/. "$sub_path"/
            echo "[session-start] Populated yaml-test-suite from the $ref release tarball (submodule clone unavailable)."
        fi
    else
        echo "[session-start] Could not populate yaml-test-suite; the Regression corpus tier will be unavailable." >&2
    fi
    rm -rf "$tmp"
}
ensure_yaml_test_suite || true

# ---------------------------------------------------------------------------
# .NET SDK and runtime
#
# global.json pins the exact SDK that CI builds with, and package feeds cannot
# supply it: Ubuntu's archive carries only the 10.0.1xx feature band. Install
# it with Microsoft's dotnet-install script, which reads the version from
# global.json, into the installation that the dotnet on PATH already uses. The
# .NET 8 runtime is installed too, so the net8.0 test legs run on .NET 8 as
# they do in CI.
# ---------------------------------------------------------------------------
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

# dotnet resolves its SDK through global.json, so `dotnet --version` succeeds
# at the repository root only when the pinned SDK is installed.
pinned_sdk_installed() {
    command -v dotnet >/dev/null 2>&1 && (cd "$repo_root" && dotnet --version >/dev/null 2>&1)
}

net8_runtime_installed() {
    command -v dotnet >/dev/null 2>&1 && dotnet --list-runtimes 2>/dev/null | grep -q '^Microsoft\.NETCore\.App 8\.'
}

# Fast path: nothing to install.
if pinned_sdk_installed && net8_runtime_installed; then
    exit 0
fi

if command -v dotnet >/dev/null 2>&1; then
    dotnet_root="$(dirname "$(readlink -f "$(command -v dotnet)")")"
else
    dotnet_root=/usr/share/dotnet
fi

install_script="$(mktemp)"
trap 'rm -f "$install_script"' EXIT
if ! curl -fsSL --retry 3 --max-time 120 https://dot.net/v1/dotnet-install.sh -o "$install_script"; then
    echo "[session-start] Could not download dotnet-install.sh. If this is a transient network issue, retry the session." >&2
    exit 1
fi

if ! pinned_sdk_installed; then
    echo "[session-start] Installing the .NET SDK pinned in global.json into $dotnet_root..."
    if ! bash "$install_script" --jsonfile "$repo_root/global.json" --install-dir "$dotnet_root" --no-path; then
        echo "[session-start] The pinned .NET SDK install failed. If this is a transient network issue, retry the session." >&2
        exit 1
    fi
fi

if ! net8_runtime_installed; then
    echo "[session-start] Installing the .NET 8 runtime into $dotnet_root..."
    bash "$install_script" --channel 8.0 --runtime dotnet --install-dir "$dotnet_root" --no-path \
        || echo "[session-start] The .NET 8 runtime install failed; the net8.0 test legs cannot run." >&2
fi

# A fresh installation is not on PATH yet.
command -v dotnet >/dev/null 2>&1 || ln -sfn "$dotnet_root/dotnet" /usr/local/bin/dotnet

# Suppress first-run telemetry/welcome work so subsequent `dotnet` commands
# don't pay that cost or write to stdout in unexpected places.
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
    {
        echo "export DOTNET_NOLOGO=1"
        echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"
        echo "export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
    } >> "$CLAUDE_ENV_FILE"
fi

echo "[session-start] dotnet $(cd "$repo_root" && dotnet --version) installed."
