---
title: Bodu licensing
---

# Bodu licensing

The Bodu suite — every primary library, every companion package, every data pack — is released under the **MIT License**.

## Summary

- **License:** [MIT](https://opensource.org/licenses/MIT)
- **Copyright:** © 2024-2026 Bodu Pty. Ltd.
- **Repository LICENSE file:** [LICENSE](https://github.com/bslater/bodu/blob/master/LICENSE)
- **NuGet metadata:** every shipped package declares `<PackageLicenseExpression>MIT</PackageLicenseExpression>` through the shared [`bld/Copyright.props`](https://github.com/bslater/bodu/blob/master/bld/Copyright.props).

## What MIT permits

The MIT License is one of the most permissive open-source licenses. You may:

- **Use** the libraries in commercial or non-commercial software.
- **Modify** the source code for your own needs.
- **Distribute** the libraries (modified or unmodified) as part of your own software.
- **Sublicense** the libraries as part of a larger work.
- **Sell** software that includes the libraries.

The only requirement is that the MIT copyright notice and permission notice be included in all copies or substantial portions of the software you distribute. The libraries are provided "as is" without warranty.

## What MIT does not require

- Sharing your modifications back to the project (though contributions are welcome).
- Disclosing your source code.
- Licensing your downstream work under MIT or any specific license.
- Attribution beyond preserving the copyright notice.

## Third-party dependencies

The Bodu libraries are intentionally light on external runtime dependencies. The few exceptions are documented per package and use compatible permissive licenses (MIT, Apache 2.0, or BSD):

| Package | External runtime dependency | License |
|---|---|---|
| `Bodu.IO.Hashing` | `System.IO.Hashing` (BCL) | MIT |
| `Bodu.Security.Cryptography` | `System.Security.Cryptography` (BCL) | MIT |
| `Bodu.Numerics.Serialization.Json`, `Bodu.Financial.Serialization.Json` | `System.Text.Json`; the Financial companion also `Microsoft.Extensions.DependencyInjection.Abstractions` | MIT |
| `Bodu.Formats.Outlook.Msg`, `Bodu.Formats.Outlook.Pst` | `System.Text.Encoding.CodePages` (legacy Windows code pages for MAPI string properties) | MIT |
| `Bodu.Extensions.Configuration.Text` | `Microsoft.Extensions.Configuration` (+ `.Binder`, `.FileExtensions`), `Microsoft.Extensions.FileProviders.Physical`, `Microsoft.Extensions.Options.ConfigurationExtensions`, `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Primitives` | MIT |
| `Bodu.Globalization.Calendar`, `Bodu.Globalization.Calendar.Plugins`, `Bodu.Financial.ExchangeRates` | `Microsoft.Extensions.Logging.Abstractions` | MIT |
| `Bodu.Globalization.Calendar.DependencyInjection`, `Bodu.Financial.DependencyInjection`, `Bodu.Globalization.Calendar.Caching`, `Bodu.Financial.ExchangeRates.Caching`, and the `Bodu.Financial.ExchangeRates.<Source>` provider packages | `Microsoft.Extensions.DependencyInjection.Abstractions`, `Microsoft.Extensions.Options` (+ `.ConfigurationExtensions`), `Microsoft.Extensions.Configuration.Abstractions` / `.Binder`, `Microsoft.Extensions.Logging.Abstractions`; the two caching cores also `Microsoft.Extensions.Hosting.Abstractions` | MIT |
| `Bodu.Financial.ExchangeRates.DependencyInjection` and the `Bodu.Financial.ExchangeRates.<Source>` provider packages | `Microsoft.Extensions.Http`, `Microsoft.Extensions.Http.Resilience` (which brings in Polly) | MIT; Polly BSD-3-Clause |
| `Bodu.Globalization.Calendar.Caching.Sqlite`, `Bodu.Financial.ExchangeRates.Caching.Sqlite` | `Microsoft.Data.Sqlite` (which brings in the `SQLitePCLRaw` bundle and the native SQLite engine) | MIT; SQLitePCLRaw Apache 2.0; SQLite public domain |
| `Bodu.Globalization.Calendar.Caching.Distributed`, `Bodu.Financial.ExchangeRates.Caching.Distributed` | `Microsoft.Extensions.Caching.Abstractions`, `Microsoft.Extensions.Caching.StackExchangeRedis` (which brings in `StackExchange.Redis`) | MIT |

Every other package depends only on other Bodu packages and the .NET 8 BCL — several layer on a sibling (`Bodu.Financial` on `Bodu.Numerics`, `Bodu.Formats.Excel.Binary` on `Bodu.IO.Compound`, `Bodu.IO.Pst` on `Bodu.Collections`, the serializers on `Bodu.Text.Serialization`), but those are all MIT-licensed Bodu code. The [package matrix](package-matrix.md) lists each package's dependencies.

## Contributing

Pull requests are welcome under the same MIT terms. By submitting a change you confirm that you have the right to license it under MIT.

## Reporting a license issue

If you find a file in the repository that appears to carry conflicting license metadata (a copyright header, third-party code with a different license, generated output, …), open an issue at <https://github.com/bslater/bodu/issues> so it can be reconciled.

## Full license text

The full MIT License text is in the repository's [LICENSE](https://github.com/bslater/bodu/blob/master/LICENSE) file and is reproduced in every distributed NuGet package.
