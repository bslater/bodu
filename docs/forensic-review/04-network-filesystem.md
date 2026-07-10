# WS-4 — Network & Filesystem / Persistence

**Scope:** `Bodu.Financial.ExchangeRates/src/` (core), the 7 provider packages `Bodu.Financial.ExchangeRates.{Boe,Ecb,Rba,Yahoo,Ofx,Xe,Oanda}/src/`, and caching `Bodu.Financial.ExchangeRates.Caching{,.Sqlite,.Distributed}/src/`.

**Overall assessment: no confirmed remotely-exploitable vulnerability.** The two items worth acting on are config-reachable defense-in-depth gaps that the codebase already handles correctly elsewhere, so aligning them is low-effort. XXE, SQL injection, TLS downgrade, ReDoS, and polymorphic-deserialization surfaces were each checked and cleared.

## Findings

| # | file:line | category | severity | status | finding | recommendation |
|---|---|---|---|---|---|---|
| 1 | `FileSystemRbaWorkbookCache.cs:61` + `RbaEraWorkbook.cs:48`; `FileSystemEcbFeedCache.cs:61`; consumed at `FileSystemByteCache{T}.cs:93,136` | Security (path traversal) | Low–Med | PLAUSIBLE | `GetFileName` returns a key-derived name (`RbaEraWorkbook.FileName` = `Label + ".xls"`, `EcbRateFeed.FileName`) that is `Path.Combine`d into the cache dir **without sanitization**. Defaults are safe literals but are overridable through provider options (`RbaRateProviderOptions.Eras`, `EcbRateProviderOptions` feeds). A configured label/filename containing `..` or an absolute/rooted path escapes the cache directory on read and write. Not remote-attacker-reachable (config only). The Caching layer, by contrast, **does** sanitize (`RateCacheFileLayout.Sanitize:163`); Boe's override is safe (date-formatted). | Sanitize the returned segment (reject/replace `Path.GetInvalidFileNameChars`, `..`, rooted paths) in `FileSystemByteCache` before combining, or validate era labels / feed names in options `Validate()`. |
| 2 | `YahooChartRateSource.cs:69` + `YahooRateProviderOptions.cs:107`; `OfxRateProviderOptions.cs:134-139` | Security (URL injection) | Low | PLAUSIBLE | Yahoo and OFX build the request **path** by raw `string.Replace` of the currency token into a template, **unescaped**. The ISO codes themselves are safe (validated enum — see cleared note), but the substituted value passes through `MapCurrency`/`BuildSymbol`, and `CurrencyAliases` **values are arbitrary config strings** placed verbatim into the path. An alias value with `/`, `?`, `#`, or `..` alters the request target. XE, OANDA, and BoE all `Uri.EscapeDataString` their mapped codes — Yahoo/OFX are the inconsistent pair. | Escape the mapped token, or validate alias values are `[A-Za-z0-9]` in `WebRateProviderOptions.TryValidate`. |
| 3 | `YahooChartRateSource.cs:54`, `OfxSpotRateHistorySource.cs:48`, `BoeCsvRateTableSource.cs:65`, `EcbXmlRateTableSource.cs:64`, `XeScrapingAuthTokenProvider.cs:165,222`, `RateProviderHttpClientFactory.cs:44` | Security (memory DoS) | Low | PLAUSIBLE | No provider sets `HttpClient.MaxResponseContentBufferSize`; every fetch uses `GetByteArrayAsync`/`GetStringAsync`/`ReadAsByteArrayAsync`, buffering the entire body into memory (default cap ~2 GB). A compromised or MITM'd endpoint could return an oversized body → OOM. XE compounds this: up to `MaxChunksScanned = 400` script chunks, each fully buffered as a string. | Set a sane `MaxResponseContentBufferSize` on provider-owned clients (and document it for DI-supplied clients), or stream + length-guard. |
| 4 | `EcbEndpointOptions.cs:65`; `BoeEndpointOptions.cs:79` | Correctness | Low | CONFIRMED (minor) | BoE joins series codes then `Uri.EscapeDataString` on the whole `a,b,c` string — the separating commas are percent-encoded (`%2C`); works only because the IADB tolerates it, brittle if codes ever need literal separators. ECB `new Uri(BaseUrl, feed.FileName)` relative-resolves, so a config `FileName` beginning `/` or containing `../` retargets the request (same config class as #1). | Escape codes individually and join with a literal `,`; validate ECB feed filenames are relative and dotless. |

## Cleared hypotheses (verified safe)

- **XXE — ECB XML parser: SAFE.** `EcbRateXmlParser.Load` uses `XDocument.Load(stream)` (`:112`) with default settings. On net8.0 the implicit `XmlReaderSettings` has `DtdProcessing = Prohibit` and `XmlResolver = null`, so external entities/DTDs are never resolved (a `DOCTYPE` throws → mapped to a format error). No `XmlResolver`/`DtdProcessing.Parse` override anywhere. Numbers/dates parsed `InvariantCulture` with positivity + ISO-code checks.
- **SQLite injection — CLEARED.** Every statement in `SqliteRateCache.cs` uses named `$` parameters (`ReadEntries:486`, `ReplaceEntries:575,586`, `ReplaceCoverage:711,722`, `BindPair:789`). The only string interpolation is the numeric `PRAGMA busy_timeout = {0}` cast to `long` (`:823-826`) — not injectable. The connection string is built via `SqliteConnectionStringBuilder` (`:164`) or taken from an explicit developer-supplied `ConnectionString`.
- **TLS — CLEARED.** No `ServerCertificateCustomValidationCallback`, `DangerousAcceptAnyServerCertificateValidator`, or `SslProtocols` downgrade anywhere in scope (grep clean).
- **ReDoS (XE) — CLEARED.** All four `[GeneratedRegex]` patterns (`XeScrapingAuthTokenProvider.cs:373-396`) are linear (`[^…]+`, `[0-9a-f]{6,}`, `[^"]*`, no nested/overlapping quantifiers). The `btoa` walk and `Basic ` scan are bounded by chunk length; chunk count capped at 400.
- **Distributed cache — CLEARED.** Key = `{prefix}{provider}:{from}{to}` (`DistributedRateCacheOptions.cs:98`); `from`/`to` are enum names, prefix/provider are config. Deserialization is non-polymorphic POCO (`JsonSerializer.Deserialize<DistributedCacheEntry>`, `:398`) with per-row `try/catch` — no type-handling RCE surface. (A writable shared Redis could still inject an oversized blob → same class as #3.)
- **Currency-code URL injection — CLEARED at the code level.** `CurrencyPair` ctor calls `ThrowIfNotDefinedCurrencyCode` (`CurrencyPair.cs:38`), so `From`/`To` are always defined enum members; `.ToString()` yields a safe 3-letter name. Residual risk is only the alias-value path (#2).
- **HttpClient per-call — CLEARED.** The single `new HttpClient()` (`RateProviderHttpClientFactory.cs:44`) is provider-owned for the provider's lifetime; the DI path uses named `IHttpClientFactory` clients. No per-request client construction.

## Hot-path notes

- `SqliteRateCache` opens a pooled connection per operation plus one keep-alive; writes loop per-row `INSERT` inside a transaction under a per-pair lock — correct, though a batched insert would cut round-trips on large ranges.
- `DistributedRateCache` does a full read-modify-write of the whole per-pair blob on every `Store`/`RecordCoverage` under a per-pair in-process lock; cross-process writes are last-write-wins (documented).
- XE token acquisition is single-flighted (`_inFlight` coalescing) and cached; the scan typically returns after the first `_app` chunk.
- All parsers fully materialize the response — see #3.

## Architecture / alignment notes

- Provider sources are consistently shaped (`IPairRateSource<T>.GetPairAsync` → build URI → fetch → parse), with endpoint/behaviour options cleanly split.
- **Escaping is not uniform**: XE/OANDA/BoE escape query params; Yahoo/OFX build paths by raw `Replace` (#2). A shared `AppendCurrency`/path-token helper on `WebRateProviderOptions` would remove the divergence.
- The `OnStorageFailureSwallowed` rate-limited-warning routine is **duplicated verbatim** between `SqliteRateCache.cs:223-240` and `DistributedRateCache.cs:349-366` (same fields, same `Interlocked` protocol). Promote to a shared helper.

## Duplication notes — reconciliation with WS-6

The "bespoke" BoE/ECB/RBA caches are **not** duplicated *implementations*: each is a thin subclass of the shared `FileSystemByteCache<T>` overriding only `GetFileName` plus a typed `TryGet`/`Store` wrapper — that is healthy reuse. What *is* triplicated is the surrounding **scaffolding**: a per-provider `I<X>Cache` interface + `Null<X>Cache` no-op + typed wrapper trio (`IBoeResponseCache`/`NullBoeResponseCache`, `IEcbFeedCache`/`NullEcbFeedCache`, `IRbaWorkbookCache`/`NullRbaWorkbookCache`). This refines WS-6 finding #2 — the consolidation is to introduce a generic `IByteCache<TKey>` + `NullByteCache<TKey>` in core and delete the three scaffolding trios, not to replace the file-cache classes themselves.

## Convention notes

Resx/convention compliance is good in scope: every exception message sampled is sourced from a `*ResourceStrings` accessor with `CultureInfo.CurrentCulture`; all wire/parse paths use `InvariantCulture`. No hard-coded throw messages seen.
