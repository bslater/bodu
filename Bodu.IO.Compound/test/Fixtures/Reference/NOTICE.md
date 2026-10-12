# Reference compound-file fixtures

These binary fixtures are third-party compound files used only to validate the
`Bodu.IO.Compound` reader against an independent corpus. They are redistributed
here under their original permissive licenses.

## Provenance

| Folder / files | Source | License |
|---|---|---|
| `valid/clean.dat`, `valid/example*.dat`, `valid/sample*.doc`, `valid/sample*.xls`, `valid/strange_*.dat`, `invalid/invalid_*.dat`, `invalid/strange_*.dat` | [`waveform-computing/compoundfiles`](https://github.com/waveform-computing/compoundfiles) - `tests/` | MIT - Copyright (c) 2014 Dave Jones |
| `valid/test-ole-file.doc` | [`decalage2/olefile`](https://github.com/decalage2/olefile) - `tests/images/` | BSD-2-Clause - Copyright (c) 2005-2023 Philippe Lagadec |

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

## Licences

The texts below are reproduced verbatim from the `LICENSE.txt` file on each
project's `master` branch, read 2026-10-12.

### compoundfiles

These terms cover every file the Provenance table attributes to
`waveform-computing/compoundfiles`.

```text
Copyright (c) 2014 Dave Jones <dave@waveform.org.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

### olefile

These terms cover `valid/test-ole-file.doc`. olefile's `LICENSE.txt` carries its
own BSD-2-Clause terms and the terms of the Python Imaging Library, whose
OleFileIO module olefile is based on.

```text
LICENSE for the olefile package:

olefile (formerly OleFileIO_PL) is copyright (c) 2005-2023 Philippe Lagadec
(https://www.decalage.info)

All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

 * Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
 * Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.


----------

olefile is based on source code from the OleFileIO module of the Python
Imaging Library (PIL) published by Fredrik Lundh under the following license:

The Python Imaging Library (PIL) is
- Copyright (c) 1997-2009 by Secret Labs AB
- Copyright (c) 1995-2009 by Fredrik Lundh

By obtaining, using, and/or copying this software and/or its associated
documentation, you agree that you have read, understood, and will comply with
the following terms and conditions:

Permission to use, copy, modify, and distribute this software and its
associated documentation for any purpose and without fee is hereby granted,
provided that the above copyright notice appears in all copies, and that both
that copyright notice and this permission notice appear in supporting
documentation, and that the name of Secret Labs AB or the author not be used
in advertising or publicity pertaining to distribution of the software without
specific, written prior permission.

SECRET LABS AB AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS
SOFTWARE, INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS. IN
NO EVENT SHALL SECRET LABS AB OR THE AUTHOR BE LIABLE FOR ANY SPECIAL,
INDIRECT OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
PERFORMANCE OF THIS SOFTWARE.
```
