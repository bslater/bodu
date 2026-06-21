# Reference compound-file fixtures

These binary fixtures are third-party compound files used only to validate the
`Bodu.IO.Compound` reader against an independent corpus. They are redistributed
here under their original permissive licenses.

## Provenance

| Folder / files | Source | License |
|---|---|---|
| `valid/clean.dat`, `valid/example*.dat`, `valid/sample*.doc`, `valid/sample*.xls`, `valid/strange_*.dat`, `invalid/invalid_*.dat`, `invalid/strange_*.dat` | [`waveform-computing/compoundfiles`](https://github.com/waveform-computing/compoundfiles) — `tests/` | MIT — Copyright (c) 2014 Dave Jones |
| `valid/test-ole-file.doc` | [`decalage2/olefile`](https://github.com/decalage2/olefile) — `tests/images/` | BSD-2-Clause — Copyright (c) 2005-2023 Philippe Lagadec |

The `invalid/` files are deliberately malformed compound files; the `valid/`
files are well-formed (or, for `strange_*`, use unusual-but-recoverable layouts).
Files whose layout the strict `Bodu.IO.Compound` reader does not support (for
example non-standard sector sizes) are placed under `invalid/`.

## Manifest

`manifest.valid.json` records the expected container facts for each valid
fixture (sector sizes, directory entries, stream sizes, and stream SHA-256
hashes, plus the root CLSID). It was generated offline by the independent
`olefile` parser; the runtime tests consume the checked-in manifest and never
require network access. Regenerate it with `download-fixtures.sh` plus the
manifest generator described there.

Expected outcomes for the malformed `invalid/` corpus are encoded in code in
`CompoundMalformedFixtures`, classified by the stable `CompoundFileError`
category the reader reports.
