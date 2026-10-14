---
uid: Bodu.Formats
---

## Purpose

**Bodu.Formats** is a namespace root: it holds no types of its own. It groups the read-only readers for application file formats, which build on the containers and codecs under <xref:Bodu.IO>:

- <xref:Bodu.Formats.Excel> - Excel binary workbooks (`.xls`, BIFF5 and BIFF8), read over `Bodu.IO.Compound` and `Bodu.IO.Biff` (**`Bodu.Formats.Excel.Binary`**).
- <xref:Bodu.Formats.Outlook> - the shared MAPI value model (**`Bodu.Formats.Outlook`**) and the `.msg` and `.pst` readers built on it (**`Bodu.Formats.Outlook.Msg`**, **`Bodu.Formats.Outlook.Pst`**).
