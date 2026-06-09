# TOML conformance corpus

`toml-test-valid.json` and `toml-test-invalid.json` are a consolidated copy of the
[toml-test](https://github.com/toml-lang/toml-test) conformance suite, vendored here as
known-answer test data for `TomlConformanceTests`.

- **valid**: each entry pairs a TOML document with its expected value tree in toml-test's
  tagged-JSON encoding (`{"type": "...", "value": "..."}`).
- **invalid**: each entry is a TOML document the parser must reject.

`TomlConformanceTests` drives the corpus through **both** specification profiles:

- **TOML v1.1.0** (`TomlReaderOptions.SpecVersion = V1_1`): every valid document must parse and
  match its expected value tree, and every invalid document must be rejected — excluding a small
  runtime skip list of cases that 1.1.0 made valid (`s_invalidSkip`).
- **Strict TOML v1.0.0** (the parser default): the entire invalid corpus, *including* the cases
  1.1.0 relaxed, must be rejected.

The following upstream cases are excluded when consolidating: `invalid/spec-1.0.0/*`
(restrictions relaxed in 1.1.0) and `invalid/encoding/*` (byte-level UTF-8 validation). The reader
now decodes streams with strict UTF-8 (invalid bytes raise `TomlFormatException`); byte-level
rejection is exercised directly by the stream encoding tests rather than this string-driven corpus.

toml-test is distributed under the MIT License; see `toml-test-LICENSE.txt`.
