"""Paths, sources of truth, and shared rules for the API documentation pipeline.

The pipeline (bld/docs/build-api-docs.sh) reads three sources of truth and never restates them:

* ``bld/release-manifest.txt`` decides which packages exist (read through ``release_manifest()`` in
  ``bld/check-docs.py``, so both tools share one grammar);
* ``$(BoduNetTargets)`` in ``bld/TargetFrameworks.props`` decides which frameworks are documented;
* MSBuild decides each package's identity and version (``bld/DocsInputs.targets``).

Everything the pipeline writes goes under ``docs/obj/api`` (ignored by git and excluded from the DocFX
build content) except the merged metadata, which goes to ``docs/api`` for ``docfx build`` to render.
"""

from __future__ import annotations

import importlib.util
import json
import os
import re
import sys
from dataclasses import dataclass
from types import ModuleType
from typing import Any

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
DOCS = os.path.join(ROOT, "docs")
OBJ = os.path.join(DOCS, "obj", "api")
API = os.path.join(DOCS, "api")
BLD_DOCS = os.path.join(ROOT, "bld", "docs")
DOCS_CHECKS = os.path.join(ROOT, "bld", "docs-checks")

PROJECTS_JSON = os.path.join(OBJ, "projects.json")
PROJECTS_PROPS = os.path.join(OBJ, "projects.props")
INPUTS_TSV = os.path.join(OBJ, "inputs.tsv")
PACKAGES_JSON = os.path.join(OBJ, "packages.json")
FRAMEWORKS_JSON = os.path.join(OBJ, "frameworks.json")
BUILD_JSON = os.path.join(OBJ, "build.json")
METADATA_CONFIG = os.path.join(OBJ, "docfx.metadata.json")
METADATA_TEMPLATE = os.path.join(DOCS, "docfx.metadata.json")
DIVERGENCE_REPORT = os.path.join(OBJ, "divergence-report.md")


def input_dir(tfm: str) -> str:
    """Returns the folder the assemblies documented for ``tfm`` are staged in."""
    return os.path.join(OBJ, "input", tfm)


def metadata_dir(tfm: str) -> str:
    """Returns the folder ``docfx metadata`` writes the ``tfm`` ManagedReference YAML to."""
    return os.path.join(OBJ, "metadata", tfm)


class PipelineError(Exception):
    """A defect in the pipeline's inputs; the message names what to fix."""


def fail(problems: list[str], title: str) -> int:
    """Prints ``problems`` as GitHub error annotations under ``title`` and returns the exit code 1."""
    print(f"{title}: {len(problems)} problem(s)", file=sys.stderr)
    for problem in problems:
        print(f"::error::{problem}", file=sys.stderr)
    return 1


def read_json(path: str) -> Any:
    """Reads a JSON file the pipeline wrote earlier, naming the stage to run when it is missing."""
    if not os.path.exists(path):
        raise PipelineError(f"{os.path.relpath(path, ROOT)} does not exist; run the earlier stages of bld/docs/build-api-docs.sh first")
    with open(path, encoding="utf-8") as stream:
        return json.load(stream)


def write_json(path: str, value: Any) -> None:
    """Writes ``value`` as stable, indented JSON with a trailing newline."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as stream:
        json.dump(value, stream, indent=2, sort_keys=False, ensure_ascii=False)
        stream.write("\n")


def check_docs() -> ModuleType:
    """Loads ``bld/check-docs.py`` (its hyphenated name rules out a plain import)."""
    path = os.path.join(ROOT, "bld", "check-docs.py")
    spec = importlib.util.spec_from_file_location("bodu_check_docs", path)
    if spec is None or spec.loader is None:
        raise PipelineError(f"cannot load {path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def bodu_net_targets() -> list[str]:
    """Returns ``$(BoduNetTargets)`` from ``bld/TargetFrameworks.props``, in declaration order."""
    path = os.path.join(ROOT, "bld", "TargetFrameworks.props")
    with open(path, encoding="utf-8") as stream:
        match = re.search(r"<BoduNetTargets>([^<]+)</BoduNetTargets>", stream.read())
    if not match:
        raise PipelineError(f"{os.path.relpath(path, ROOT)} does not declare <BoduNetTargets>")
    return [tfm.strip() for tfm in match.group(1).split(";") if tfm.strip()]


@dataclass(frozen=True, order=True)
class Framework:
    """A documented target framework and the names a reader sees for it."""

    major: int
    minor: int
    tfm: str

    @property
    def product(self) -> str:
        """The product column of an Applies-to table."""
        return ".NET"

    @property
    def version(self) -> str:
        """The version column entry: ``10`` for ``net10.0``, ``10.1`` for a hypothetical ``net10.1``."""
        return str(self.major) if self.minor == 0 else f"{self.major}.{self.minor}"

    @property
    def label(self) -> str:
        """The name shown in the framework selector and in availability notes, e.g. ``.NET 10``."""
        return f"{self.product} {self.version}"

    @property
    def view(self) -> str:
        """The ``?view=`` token for this framework, e.g. ``net-10.0``."""
        return f"net-{self.major}.{self.minor}"

    def to_json(self) -> dict[str, str]:
        """Returns the record the templates read from ``_boduFrameworks``."""
        return {"tfm": self.tfm, "product": self.product, "version": self.version, "label": self.label, "view": self.view}


_NET_TFM = re.compile(r"^net(\d+)\.(\d+)$")


def parse_framework(tfm: str) -> Framework:
    """Parses a modern .NET target framework moniker (``netX.Y``); anything else is an error."""
    match = _NET_TFM.match(tfm)
    if not match:
        raise PipelineError(f"'{tfm}' is not a .NET target framework moniker of the form netX.Y; the API reference documents only those")
    return Framework(int(match.group(1)), int(match.group(2)), tfm)


def frameworks_newest_first(tfms: list[str]) -> list[Framework]:
    """Returns the frameworks newest first, the order in which their metadata takes precedence."""
    return sorted((parse_framework(tfm) for tfm in tfms), reverse=True)
