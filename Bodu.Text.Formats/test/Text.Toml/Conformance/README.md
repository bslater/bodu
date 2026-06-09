# TOML conformance corpus

`toml-test-valid.json` and `toml-test-invalid.json` are a consolidated copy of the
[toml-test](https://github.com/toml-lang/toml-test) conformance suite, vendored here as
known-answer test data for `TomlConformanceTests`.

- **valid**: each entry pairs a TOML document with its expected value tree in toml-test's
  tagged-JSON encoding (`{"type": "...", "value": "..."}`).
- **invalid**: each entry is a TOML document the parser must reject.

Together with the `encoding/` files described under [Exclusions](#exclusions), this vendors every
upstream `toml-test` case for both spec versions.

## Per-version gating

`toml-test-files-1.0.0.txt` and `toml-test-files-1.1.0.txt` are toml-test's own
`files-toml-<version>` manifests, vendored verbatim. Each lists the test files that make up that
version's suite, so they are the authoritative source for which documents a parser at that version
must accept or reject. `TomlConformanceTests` keys each corpus case by its toml-test path (the part
under `valid/` or `invalid/`, without the `.toml` extension) and gates each spec against its own
manifest:

| Suite | Documents | Requirement |
|---|---:|---|
| v1.1.0 valid | 266 | parse under `SpecVersion = V1_1` and match the expected tree (v1.1.0 is a superset, so it accepts every valid document) |
| v1.0.0 valid | 209 | parse under the strict v1.0.0 default and match the expected tree |
| v1.1.0 invalid | 473 | rejected under `SpecVersion = V1_1` |
| v1.0.0 invalid | 480 | rejected under the strict v1.0.0 default |

Two guards keep the corpus and manifests in sync, so drift in either direction surfaces on the next
re-vendor rather than silently dropping coverage:

- `Manifest_WhenLoaded_ShouldClassifyEveryCorpusCase` fails if any vendored case is missing from both
  manifests.
- `Manifest_WhenLoaded_ShouldVendorEveryManifestCase` fails if any manifest case is missing from the
  corpus, other than the documented exclusions below.

The version *boundary* itself — that strict v1.0.0 rejects the specific v1.1.0 additions (`\e` /
`\xHH`, optional seconds, multi-line / trailing-comma inline tables) — is asserted directly by
`TomlReaderVersionTests`.

## Encoding cases

The `invalid/encoding/*` cases — ill-formed UTF-8, UTF-16 input, and misplaced byte-order marks —
have no JSON-string representation, so they are excluded from the two consolidated JSON files and
instead vendored verbatim as raw bytes under `encoding/`. `TomlEncodingConformanceTests` reads each
as a stream and asserts rejection under both the strict v1.0.0 default and the v1.1.0 profile, and a
guard keeps those resources in exact correspondence with the manifests' `encoding/*` entries. The
JSON corpus's reverse drift guard therefore treats the `encoding/` prefix as its sole permitted
exclusion, because that coverage lives in the binary resources instead.

## Refreshing

To re-vendor, copy `toml-test`'s `tests/files-toml-1.0.0` and `tests/files-toml-1.1.0` into the
`*.txt` files here, regenerate the two JSON corpora from `tests/valid` and `tests/invalid` (excluding
`invalid/encoding/*`), and copy `tests/invalid/encoding/*.toml` verbatim into `encoding/`. The
manifests are the source of truth for the per-version split; no skip list is maintained by hand, and
the drift guards fail if the corpus, the encoding resources, and the manifests fall out of sync.

toml-test is distributed under the MIT License; see `toml-test-LICENSE.txt`. The vendored
`files-toml-*` manifests are part of toml-test and carry the same license.
