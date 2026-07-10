# WS-5 — Plugin Trust Boundary

**Scope:** `Bodu.Globalization.Calendar.Plugins/src/` — trust-gated external assembly loading for custom `INotableDateAlgorithm` implementations.

**Overall assessment: well-designed.** The core `LoadFrom(string)` trust boundary could not be faulted; the findings are a fail-open combinator, a weaker secondary overload, and two minor items.

## Verified good (cleared)

- **No hash TOCTOU on the file-path overload.** The image is read once into memory (`NotableDatePluginLoader.cs:126`), SHA-256'd from those exact bytes (`:127`), and the *same* `MemoryStream` is mapped via `LoadFromStream` (`:135-136`). The digest the policy checks is the digest of the bytes actually loaded.
- **Trust is evaluated before any plugin code runs.** `EvaluateTrustAndActivate` calls `trustPolicy.Evaluate` at `:204` and only reaches `Activator.CreateInstance` at `:220/:237` after `IsTrusted` passes. `LoadFromStream` maps but does not execute; the post-gate custom-attribute read only instantiates a loader-owned attribute token, not plugin code.
- **ALC is collectible and unloaded on rejection/failure.** `isCollectible: true` (`:132`); the `catch` unloads on *any* throw, including `PluginNotTrustedException` (`:144-148`).
- **File-hash comparison is constant-time and byte-exact** via `CryptographicOperations.FixedTimeEquals` (`FileHashPolicy:71`) — no string/case/truncation path.
- **Strong-name comparison is correct** and honestly documents that it verifies the manifest token, *not* the signature (`StrongNamePolicy:22-24`).
- **No accidental allow-all default.** The loader has no DI/options wiring; `trustPolicy` is a required, null-checked argument. `AllowAllPluginTrustPolicy` is clearly documented dev-only (`:12-17`).
- Messages sourced from `PluginsResourceStrings` with `CultureInfo.CurrentCulture`; source-generated `Log` short-circuits on `NullLogger`; XML docs complete.

## Findings

| # | file:line | category | severity | status | finding | recommendation |
|---|---|---|---|---|---|---|
| 1 | `CompositePluginTrustPolicy.cs:24-45` | Security | Medium | CONFIRMED | The `params IPluginTrustPolicy[]` constructor accepts **zero** policies. `new CompositePluginTrustPolicy()` produces a policy whose `foreach` body never runs and returns `PluginTrustResult.Trusted()` — a **fail-open** that trusts every assembly. A misconfiguration (e.g. a filtered/empty list splatted in) silently disables the gate. | Reject an empty/all-null policy set in the constructor (`ArgumentException`), or make `Evaluate` return `Rejected` when `_policies.Length == 0`. Fail closed. |
| 2 | `NotableDatePluginLoader.cs:80-94` | Security | Medium | CONFIRMED | The **`LoadFrom(Assembly)`** overload offers materially weaker guarantees than its docs imply. (a) The assembly is *already loaded* before trust is evaluated, so its module initializer / type code may already have executed. (b) The hash is computed by **re-reading the file from disk** (`:90` → `ComputeHash`, `:262-266`), not from the loaded image, reintroducing exactly the TOCTOU the string overload's comment (`:123-125`) warns against. The method's own docs say trust is checked "before activation" without flagging that for a pre-loaded assembly this is advisory only. | Add an explicit `<remarks>` warning that this overload trusts an already-loaded assembly (code may have run) and that its `FileHash` reflects on-disk bytes, not the loaded image. Steer consumers to the path overload for untrusted input; consider tagging the reflected-hash provenance in `PluginTrustContext`. |
| 3 | `NotableDatePluginLoader.cs:133-143` | Architecture | Low | CONFIRMED | On the **success** path the collectible `AssemblyLoadContext` is never surfaced or retained — the returned `INotableDatePlugin` carries no handle to its ALC. A successfully loaded plugin can therefore never be unloaded (defeating `isCollectible: true` for the common case) and the context/assembly is pinned for process life. | Return or expose the ALC (e.g. a disposable load handle wrapping plugin + context) so hosts can unload trusted plugins, mirroring the rejection path's cleanup. |
| 4 | `NotableDatePluginLoader.cs:239` | Correctness | Low | PLAUSIBLE | The activation `catch` filter is `MissingMethodException or TargetInvocationException or MemberAccessException`. A plugin default constructor that throws a *non-wrapped* exception type not in this set would propagate raw rather than as the contracted `PluginActivationException`. `Activator.CreateInstance(Type)` normally wraps ctor throws in `TargetInvocationException`, so this is edge-case, but the filter is narrower than "any activation failure." | Confirm the intended contract; if `PluginActivationException` is meant to envelope all activation failures, broaden the filter (while still letting `OutOfMemoryException`/`StackOverflowException` escape). |

## Architecture / alignment notes

- The design cleanly separates the trust *context* (`PluginTrustContext`), *decision* (`IPluginTrustPolicy` implementations), and *loading/activation* (`NotableDatePluginLoader`). Policies are pure and side-effect-free.
- `PluginTrustContext.FileHash` provenance is ambiguous across the two overloads (loaded-image bytes vs. disk re-read). A policy cannot distinguish them, which is the root of finding #2. Tagging provenance would let a strict policy reject the reflected-hash case.
- The one genuinely arbitrary-code surface — the plugin constructor and `GetAlgorithms()` — runs only after the gate, which is correct.

## Duplication notes

- SHA-256 computation exists twice: over the in-memory image (`:127`) and over a `FileStream` (`:262-266`). This is intentional (two overloads), but it is the mechanism behind finding #2, so the divergence warrants a provenance flag rather than consolidation.
- `FormatPublicKeyToken` (`:273`) and the two policies' normalization (`ToLowerInvariant` + `OrdinalIgnoreCase`) are consistent — token comparison is not double-normalized inconsistently.
