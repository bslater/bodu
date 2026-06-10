#!/bin/bash
# Installs the .NET SDK 10.0 on session start so the agent can run `dotnet
# build` and `dotnet test` against bodu.slnx, and repairs the dotnet-dnceng
# Claude Code plugin whose upstream manifest currently fails validation (see
# repair_dnceng_plugin below). Only runs in the remote Claude Code on the web
# environment; on a developer's local machine the SDK is expected to be
# installed already.
#
# SDK 10.0 is required because the solution uses C# 14 language features (for
# example the `field` keyword on semi-auto properties). An older SDK such as
# 8.0 cannot compile the sources.
#
# The script is idempotent: when a .NET 10 SDK is already installed it exits
# immediately, so re-invocation (resume, clear, compact) is essentially free.
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

# Fast path: a .NET 10 SDK is already installed. Checking for the 10.x band
# specifically (rather than merely `dotnet` on PATH) ensures we still upgrade
# a container image that ships only an older SDK such as 8.0.
if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; then
    exit 0
fi

echo "[session-start] .NET 10 SDK not found; installing dotnet-sdk-10.0 via apt..."

export DEBIAN_FRONTEND=noninteractive

# Refresh apt only if no recent lists are present, to keep re-runs cheap.
# Tolerate third-party PPA failures (deadsnakes, ondrej, etc.) — the main
# Ubuntu archive is sufficient for dotnet-sdk-10.0.
if [ -z "$(find /var/lib/apt/lists -maxdepth 1 -type f -mmin -1440 2>/dev/null)" ]; then
    apt-get update -qq || echo "[session-start] apt-get update reported errors; continuing with existing cache."
fi

if ! apt-get install -y --no-install-recommends dotnet-sdk-10.0; then
    echo "[session-start] dotnet-sdk-10.0 install failed. If this is a transient cache issue, retry the session." >&2
    exit 1
fi

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

echo "[session-start] dotnet $(dotnet --version) installed."
