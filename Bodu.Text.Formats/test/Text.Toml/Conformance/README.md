# TOML conformance corpus

`toml-test-valid.json` and `toml-test-invalid.json` are a consolidated copy of the
[toml-test](https://github.com/toml-lang/toml-test) conformance suite, vendored here as
known-answer test data for `TomlConformanceTests`.

- **valid**: each entry pairs a TOML document with its expected value tree in toml-test's
  tagged-JSON encoding (`{"type": "...", "value": "..."}`).
- **invalid**: each entry is a TOML document the parser must reject.

Targeting **TOML v1.1.0**, the following upstream cases are excluded when consolidating:
`invalid/spec-1.0.0/*` (restrictions relaxed in 1.1.0) and `invalid/encoding/*` (byte-level
UTF-8 validation; the stream reader decodes with spec-permitted U+FFFD replacement). A small
runtime skip list in `TomlConformanceTests` covers individual cases that 1.1.0 made valid.

toml-test is distributed under the MIT License; see `toml-test-LICENSE.txt`.
