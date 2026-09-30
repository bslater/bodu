"""Reads and writes DocFX ManagedReference YAML without changing what any value means.

DocFX writes its metadata with YamlDotNet, which follows the YAML 1.2 core schema. PyYAML follows
YAML 1.1, whose implicit types disagree with it: under 1.1 a plain ``=`` is the special "value" tag
(DocFX writes ``name.vb: =`` for the VB name of ``operator ==``, and PyYAML cannot construct it),
``012`` is octal, and ``On`` / ``Yes`` / ``No`` are booleans. Round-tripping DocFX output through
PyYAML's defaults would therefore fail on some files and silently retype values in others.

This module gives PyYAML the YAML 1.2 core schema instead, for loading and dumping alike: a plain
scalar is null (``~``, ``null``, ``Null``, ``NULL`` or empty), a boolean (``true`` / ``false`` in its
three spellings), a decimal integer, a float, or otherwise a string. Because the dumper resolves with
the same rules, a string that would read back as anything else is quoted, and every other string is
written plainly, as YamlDotNet writes it.

The libyaml-backed loader and dumper are used when PyYAML was built with them; the schema is the
same either way.
"""

from __future__ import annotations

import re
from typing import Any

import yaml

MANAGED_REFERENCE_HEADER = "### YamlMime:ManagedReference"
TOC_HEADER = "### YamlMime:TableOfContent"

_BaseLoader = getattr(yaml, "CSafeLoader", yaml.SafeLoader)
_BaseDumper = getattr(yaml, "CSafeDumper", yaml.SafeDumper)

# The YAML 1.2 core schema (https://yaml.org/spec/1.2.2/#1032-tag-resolution), in the order the
# resolvers are tried. Each entry is (tag, pattern, first characters the pattern can start with).
_CORE_SCHEMA: list[tuple[str, re.Pattern[str], str]] = [
    ("tag:yaml.org,2002:null", re.compile(r"^(?:~|null|Null|NULL|)$"), "~nN"),  # "" is added below
    ("tag:yaml.org,2002:bool", re.compile(r"^(?:true|True|TRUE|false|False|FALSE)$"), "tTfF"),
    ("tag:yaml.org,2002:int", re.compile(r"^[-+]?[0-9]+$"), "-+0123456789"),
    (
        "tag:yaml.org,2002:float",
        re.compile(r"^(?:[-+]?(?:\.[0-9]+|[0-9]+(?:\.[0-9]*)?)(?:[eE][-+]?[0-9]+)?|[-+]?\.(?:inf|Inf|INF)|\.(?:nan|NaN|NAN))$"),
        "-+.0123456789",
    ),
]


def _install_core_schema(cls: type) -> None:
    """Replaces the YAML 1.1 implicit resolvers of ``cls`` with the YAML 1.2 core schema."""
    cls.yaml_implicit_resolvers = {}
    for tag, pattern, first in _CORE_SCHEMA:
        # PyYAML indexes resolvers by a scalar's first character, and the empty scalar under "".
        starts = list(first) + ([""] if tag.endswith(":null") else [])
        cls.add_implicit_resolver(tag, pattern, starts)


class Loader(_BaseLoader):  # type: ignore[misc, valid-type]
    """A safe loader that resolves plain scalars with the YAML 1.2 core schema."""


class Dumper(_BaseDumper):  # type: ignore[misc, valid-type]
    """A safe dumper that quotes exactly the strings the YAML 1.2 core schema would retype."""

    def ignore_aliases(self, data: Any) -> bool:
        # Write every value in full. DocFX's own output never uses anchors, and a value the merge
        # shares between two items must not come out as an &anchor / *alias pair.
        return True


_install_core_schema(Loader)
_install_core_schema(Dumper)


def _construct_int(loader: yaml.BaseLoader, node: yaml.ScalarNode) -> int:
    # YAML 1.2 core integers are decimal: "012" is twelve, not the YAML 1.1 octal ten.
    return int(loader.construct_scalar(node), 10)


def _construct_bool(loader: yaml.BaseLoader, node: yaml.ScalarNode) -> bool:
    return loader.construct_scalar(node).lower() == "true"


Loader.add_constructor("tag:yaml.org,2002:int", _construct_int)
Loader.add_constructor("tag:yaml.org,2002:bool", _construct_bool)


def _represent_str(dumper: yaml.BaseDumper, value: str) -> yaml.ScalarNode:
    # Multi-line text (summaries, remarks, examples) reads best as a literal block; the emitter falls
    # back to a quoted style by itself when the text cannot be written as one.
    style = "|" if "\n" in value else None
    return dumper.represent_scalar("tag:yaml.org,2002:str", value, style=style)


Dumper.add_representer(str, _represent_str)


def load(text: str) -> Any:
    """Parses one YAML document with the YAML 1.2 core schema."""
    return yaml.load(text, Loader=Loader)  # noqa: S506 - Loader is a SafeLoader subclass


def load_file(path: str) -> Any:
    """Parses the YAML document in ``path`` with the YAML 1.2 core schema."""
    with open(path, encoding="utf-8") as stream:
        return load(stream.read())


def dump(data: Any, header: str | None = None) -> str:
    """Serializes ``data`` deterministically, preceded by ``header`` (a DocFX YamlMime line) if given."""
    body = yaml.dump(
        data,
        Dumper=Dumper,
        sort_keys=False,
        allow_unicode=True,
        default_flow_style=False,
        width=2**31 - 1,
        indent=2,
    )
    return f"{header}\n{body}" if header else body


def dump_file(path: str, data: Any, header: str | None = None) -> None:
    """Writes ``data`` to ``path`` as UTF-8 with LF line endings."""
    with open(path, "w", encoding="utf-8", newline="\n") as stream:
        stream.write(dump(data, header))


def read_header(path: str) -> str | None:
    """Returns the ``### YamlMime:`` line that opens ``path``, or ``None`` when it has none."""
    with open(path, encoding="utf-8") as stream:
        first = stream.readline().rstrip("\r\n")
    return first if first.startswith("### YamlMime:") else None
