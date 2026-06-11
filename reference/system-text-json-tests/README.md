# System.Text.Json test corpus — reference only

This directory holds the test sources from the .NET runtime's `System.Text.Json.Tests`
project, carried into this branch as a **reference corpus** for the Bodu serialization
test-mirroring effort described in [`../REMAINING-TEST-PLAN.md`](../REMAINING-TEST-PLAN.md).
The `Bodu.Text.Bencode` and `Bodu.Text.Toml` test suites are authored to mirror these
scenarios at per-dimension depth.

## Why it is here
The original upload of this corpus cannot be reproduced in later sessions, so it is
committed to the working branch to carry the reference forward. It supplements (does not
replace) the distilled scenario catalogs, data, and probed contracts in
`/REMAINING-TEST-PLAN.md`.

## Layout
- `Common/` — the shared abstract test bodies that hold the real `[Fact]`/`[Theory]`
  methods: `ConstructorTests/` (`*.ParameterMatching/.AttributePresence/.Exceptions`),
  `CollectionTests/`, `ExtensionDataTests.cs`, `PropertyVisibilityTests.cs`,
  `NumberHandlingTests.cs`, `JsonCreationHandlingTests.*`, `UnsupportedTypesTests.cs`, …
- `System.Text.Json.Tests/` — reader/writer tests (`Utf8Json{Reader,Writer}Tests*`), the
  DOM tests (`JsonNode/`, `JsonDocumentTests.cs`, `JsonElement*`), the serialization
  feature tests (`Serialization/`), and the thin per-serializer subclasses that delegate
  into `Common/`.

## NOT product code
- Reference material on branch `claude/relaxed-rubin-m62s2x` only.
- **Not compiled:** it lives outside every Bodu project directory, so no `.csproj` globs
  it; DocFX globs only `.md`/`.yml`; NuGet packages build from specific `src/` projects.
- The original `.csproj`/`.proj` build files were renamed to `*.txt` so nothing attempts
  to build them.
- **Remove this directory before merging to `master` / before any release.**

## Provenance & license
- Source: `dotnet/runtime` —
  `src/libraries/System.Text.Json/tests/System.Text.Json.Tests` (plus the shared
  `Common/` test bodies).
- Copyright (c) .NET Foundation and Contributors.
- Licensed under the **MIT License**; each file retains its original MIT header.

```
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

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
