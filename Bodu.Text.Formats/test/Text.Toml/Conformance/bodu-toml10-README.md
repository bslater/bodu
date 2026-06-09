# TOML 1.0 Corpus

This is a small, self-contained TOML 1.0.0-focused corpus intended to supplement, not replace, the official `toml-test` suite.

Structure:

- `valid/*.toml` — TOML documents that a TOML 1.0.0 parser should accept.
- `valid/*.json` — expected output using the `toml-test` tagged JSON convention.
- `invalid/*.toml` — TOML documents that a strict TOML 1.0.0 parser should reject.
- `manifest.json` — case metadata, tags, and intended assertions.

Tagged JSON convention:

- TOML tables map to JSON objects.
- TOML arrays map to JSON arrays.
- Scalar values map to `{ "type": "...", "value": "..." }`.

The cases intentionally include TOML 1.1 syntax that must be rejected by a TOML 1.0.0 mode, such as `\e`, `\xHH`, optional seconds, and multi-line/trailing-comma inline tables.
