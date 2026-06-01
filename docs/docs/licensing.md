---
title: Bodu licensing
---

# Bodu licensing

The Bodu suite — every primary library, every companion package, every data pack — is released under the **MIT License**.

## Summary

- **License:** [MIT](https://opensource.org/licenses/MIT)
- **Copyright:** © 2024-2026 Bodu Pty. Ltd.
- **Repository LICENSE file:** [LICENSE](https://github.com/bslater/bodu/blob/master/LICENSE)
- **NuGet metadata:** every shipped package declares `<PackageLicenseExpression>MIT</PackageLicenseExpression>` in [`bld/Bodu.props`](https://github.com/bslater/bodu/blob/master/bld/Bodu.props).

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
| `Bodu.Extensions.Configuration.Text` | `Microsoft.Extensions.Configuration` | MIT |
| `Bodu.Globalization.Calendar.DependencyInjection` | `Microsoft.Extensions.DependencyInjection.Abstractions` | MIT |

All other primary libraries depend only on `Bodu.Core` and the .NET 8 BCL.

## Contributing

Pull requests are welcome under the same MIT terms. By submitting a change you confirm that you have the right to license it under MIT.

## Reporting a license issue

If you find a file in the repository that appears to carry conflicting license metadata (a copyright header, third-party code with a different license, generated output, …), open an issue at <https://github.com/bslater/bodu/issues> so it can be reconciled.

## Full license text

The full MIT License text is in the repository's [LICENSE](https://github.com/bslater/bodu/blob/master/LICENSE) file and is reproduced in every distributed NuGet package.
