# Bodu.Numerics.Currencies

This folder holds the ISO 4217 currency tag types that parameterise
`Money<TCurrency>`. Each `<ISO>.cs` file declares a single sealed class
implementing `ICurrency` and is regenerated from `currencies.json` —
**do not edit the `.cs` files directly**.

## Regeneration

Edit `currencies.json` to add a currency, change the minor-unit precision
of an existing entry, or update the display name. Then regenerate:

```bash
dotnet run --project tools/CurrencyCatalogueGenerator
```

The tool reads `currencies.json`, validates each entry, emits one file per
currency, and deletes any stale `.cs` file whose ISO code no longer
appears in the input. The output is deterministic; re-running with no
changes to `currencies.json` produces no diff.

## `currencies.json` format

Each row is `{ "iso": "USD", "numeric": 840, "minorUnits": 2, "name": "US Dollar" }`.

- `iso` — three-letter uppercase ISO 4217 alphabetic code.
- `numeric` — ISO 4217 numeric code (000–999).
- `minorUnits` — the currency's fractional-digit precision (0, 2, or 3 for active currencies).
- `name` — short English name; appears in the generated XML doc comment.

The shipped catalogue covers active ISO 4217 currencies. Special-purpose
codes (precious metals such as `XAU`/`XAG`, `XDR`, testing codes such as
`XTS`, the no-currency code `XXX`) are intentionally excluded; add them
to `currencies.json` if your scenario requires them.
