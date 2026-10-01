# Bodu

A suite of small, focused .NET libraries for collections, non-cryptographic hashing, cryptography,
calendar computation, binary-to-text encoding, document formats, serialization, configuration,
numerics, and money.

Every package shares one solution, one set of conventions, and one bar for quality: nullable reference
types enabled, analyzer-clean under warnings-as-errors, deterministic and strong-name-signed builds,
SourceLink, and framework-style XML documentation on every public member. The libraries target
`net8.0` and `net10.0` and are released under the [MIT License](LICENSE).

- **Documentation:** <https://bslater.github.io/bodu/>
- **Getting started:** [cross-library tour](https://bslater.github.io/bodu/docs/getting-started.html)
- **Package matrix:** [every package, what it depends on, and its API-stability tier](https://bslater.github.io/bodu/docs/package-matrix.html)

## Packages

Each package is installed on its own; install only the ones you need. `Bodu.Core` is the common
foundation and is pulled in transitively.

```bash
dotnet add package Bodu.Collections
```

| Topic | Packages |
|---|---|
| **Core foundations** | [`Bodu.Core`](https://www.nuget.org/packages/Bodu.Core), [`Bodu.Collections`](https://www.nuget.org/packages/Bodu.Collections), [`Bodu.Collections.Concurrent`](https://www.nuget.org/packages/Bodu.Collections.Concurrent) |
| **Hashing and cryptography** | [`Bodu.IO.Hashing`](https://www.nuget.org/packages/Bodu.IO.Hashing), [`Bodu.Security.Cryptography`](https://www.nuget.org/packages/Bodu.Security.Cryptography) |
| **Globalization and calendars** | [`Bodu.Globalization.Calendar`](https://www.nuget.org/packages/Bodu.Globalization.Calendar) with its companions and regional data packs, [`Bodu.Globalization.Recurrence`](https://www.nuget.org/packages/Bodu.Globalization.Recurrence) |
| **Text and serialization** | [`Bodu.Text.Encoding`](https://www.nuget.org/packages/Bodu.Text.Encoding), [`Bodu.Text.Filtering`](https://www.nuget.org/packages/Bodu.Text.Filtering), [`Bodu.Text.Formats`](https://www.nuget.org/packages/Bodu.Text.Formats) (Delimited, DotEnv, INI), [`Bodu.Text.Bencode`](https://www.nuget.org/packages/Bodu.Text.Bencode), [`Bodu.Text.Toml`](https://www.nuget.org/packages/Bodu.Text.Toml), [`Bodu.Text.Yaml`](https://www.nuget.org/packages/Bodu.Text.Yaml) |
| **Configuration** | [`Bodu.Text.Configuration`](https://www.nuget.org/packages/Bodu.Text.Configuration), [`Bodu.Extensions.Configuration.Text`](https://www.nuget.org/packages/Bodu.Extensions.Configuration.Text) |
| **Numerics and financial** | [`Bodu.Numerics`](https://www.nuget.org/packages/Bodu.Numerics), [`Bodu.Financial`](https://www.nuget.org/packages/Bodu.Financial) with its serialization, dependency-injection and exchange-rate companions |
| **Binary formats and I/O** | [`Bodu.IO.Compound`](https://www.nuget.org/packages/Bodu.IO.Compound), [`Bodu.IO.Biff`](https://www.nuget.org/packages/Bodu.IO.Biff), [`Bodu.Formats.Excel.Binary`](https://www.nuget.org/packages/Bodu.Formats.Excel.Binary), [`Bodu.IO.Pst`](https://www.nuget.org/packages/Bodu.IO.Pst), [`Bodu.Formats.Outlook.Msg`](https://www.nuget.org/packages/Bodu.Formats.Outlook.Msg), [`Bodu.Formats.Outlook.Pst`](https://www.nuget.org/packages/Bodu.Formats.Outlook.Pst) |

Every package's own `README.md` (shown on its NuGet page) states its API-stability tier. **Stable**
packages follow semantic versioning from 1.0; **Preview** packages stay below 1.0 while their API
settles, and may change between minor versions.

## Building from source

The repository builds with the .NET SDK that [`global.json`](global.json) pins (a .NET 10 SDK), and
runs the `net8.0` test legs on the .NET 8 runtime.

```bash
dotnet build bodu.slnx
dotnet test  bodu.slnx --settings bvt.runsettings     # the fast default tier
dotnet test  bodu.slnx --settings test.runsettings    # what CI runs, including the Regression tier
```

[CONTRIBUTING.md](CONTRIBUTING.md) covers the prerequisites, the test tiers, how to reproduce CI
locally, and the conventions a change is reviewed against.

## Contributing

Bug reports, documentation fixes, and pull requests are welcome. Please read
[CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request, and follow the
[Code of Conduct](CODE_OF_CONDUCT.md) in every project space.

## Security

Please do **not** report security vulnerabilities through public issues. [SECURITY.md](SECURITY.md)
explains how to report one privately and which versions receive fixes.

## License

Copyright (c) 2024-2026 Bodu Pty. Ltd. Released under the [MIT License](LICENSE).
