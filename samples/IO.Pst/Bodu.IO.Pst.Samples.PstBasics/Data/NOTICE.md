# Sample data provenance

`sample1.pst` (Unicode format, `wVer` 23) and `sample2.pst` (ANSI format,
`wVer` 14) are third-party PST files from the Microsoft **pstsdk** (PST File
Format SDK) test corpus, redistributed under the Apache License 2.0 (see
[`LICENSE.Apache-2.0.txt`](LICENSE.Apache-2.0.txt); Copyright Microsoft /
Terry Mahaffey). They are copies of
`Bodu.IO.Pst/test/Fixtures/Reference/unicode/sample1.pst` and
`Bodu.IO.Pst/test/Fixtures/Reference/ansi/sample2.pst`, whose full
provenance — source mirror, retrieval date, and SHA-256 pins — is recorded
in [`Bodu.IO.Pst/test/Fixtures/Reference/NOTICE.md`](../../../../Bodu.IO.Pst/test/Fixtures/Reference/NOTICE.md).

The copies exist so the sample runs offline out of the box; point the
scenarios at any other `.pst` of either format by editing `Program.cs`.
