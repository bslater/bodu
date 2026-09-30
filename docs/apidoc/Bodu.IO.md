---
uid: Bodu.IO
---

## Purpose

**Bodu.IO** is a namespace root: it holds no types of its own. It groups the byte-level I/O libraries, each shipped as its own package:

- <xref:Bodu.IO.Hashing> - non-cryptographic hashes, checksums, and check digits (**`Bodu.IO.Hashing`**).
- <xref:Bodu.IO.Compound> - the OLE2 Compound File Binary container used by legacy Office files (**`Bodu.IO.Compound`**).
- <xref:Bodu.IO.Biff> - the Excel BIFF5 / BIFF8 record-stream codec (**`Bodu.IO.Biff`**).
- <xref:Bodu.IO.Pst> - the Outlook personal-folders node and heap layers (**`Bodu.IO.Pst`**).

The format readers built on them live under <xref:Bodu.Formats>.
