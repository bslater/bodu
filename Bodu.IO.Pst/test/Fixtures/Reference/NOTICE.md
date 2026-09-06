# Reference PST fixtures (seed corpus)

These binary fixtures are third-party PST files acquired ahead of the
`Bodu.IO.Pst` implementation — they resolve the fixture-acquisition risk
(R2) recorded in
[`../../../docs/pst-container-exploration.md`](../../../docs/pst-container-exploration.md)
and anchor the future P0 spike. They are redistributed here under their
original permissive license, whose full text is carried alongside in
[`LICENSE.Apache-2.0.txt`](LICENSE.Apache-2.0.txt). No test project
consumes them yet; the P0 spike wires them.

## Provenance

| Folder / files | Source | License |
|---|---|---|
| `unicode/sample1.pst`, `unicode/test_unicode.pst`, `ansi/sample2.pst`, `ansi/test_ansi.pst` | Microsoft **pstsdk** (PST File Format SDK) test corpus — retrieved 2026-07-31 from the [`emk/pstsdk`](https://github.com/emk/pstsdk) mirror of the original `pstsdk.codeplex.com` SVN, `test/` | Apache-2.0 — Copyright Microsoft / Terry Mahaffey. The mirror carries no license file; pstsdk was distributed under the Apache License 2.0 on CodePlex, which is the basis for redistribution here. |

## File facts (verified on acquisition)

| File | Header | Format | SHA-256 |
|---|---|---|---|
| `unicode/sample1.pst` | `!BDN`, `wVer` 23 | Unicode | `9e77f0f7937768506f85eb33b7c114e23ddf3bc7fd5d85226fce56586a8e3618` |
| `unicode/test_unicode.pst` | `!BDN`, `wVer` 23 | Unicode | `3f2b8ebf011ca754b9c2017d264423e173ef1407f460bd4cefde4a6ccc041f8b` |
| `ansi/sample2.pst` | `!BDN`, `wVer` 14 | ANSI | `587a1ae2785eb218d7d42a98d43ded546aa72670908269d7b3ab25a711a91871` |
| `ansi/test_ansi.pst` | `!BDN`, `wVer` 14 | ANSI | `f1ff591b7441f8fcf78cc6c65a78c5d168763338f2fcdf3df5745e742bd66d04` |

The `unicode/` files were the P0 initial scope; the `ansi/` files drive the
ANSI-format reader tests (header, corpus walk, `lspst` oracle rows, and the
malformed-input sweeps) now that both formats are read.

## Manifest

[`manifest.seed.json`](manifest.seed.json) records, per file, the header
facts above plus a content listing produced by an **independent
implementation** — `lspst` from libpst (`pst-utils`) — giving the P0
spike ready-made cross-implementation expectations (folder names,
message counts, sender, subject).

## Future scale tier

The EDRM Enron v2 dataset (CC-BY, tens-of-megabyte PSTs) is the noted
candidate for a large-file streaming tier once `Bodu.IO.Pst` exists; it
is deliberately not checked in here.
