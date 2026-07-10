# WS-2 — Untrusted-Input Parsers

**Packages:** `Bodu.IO.Compound/src/`, `Bodu.Formats.Excel.Binary/src/`, `Bodu.Text.Bencode/src/`, `Bodu.Text.Toml/src/`, `Bodu.Text.Yaml/src/`, `Bodu.Text.Formats/src/`, `Bodu.Text.Encoding/src/`, `Bodu.Text.Configuration/src/`.

**Threat model:** all input is attacker-controlled bytes/text.

**Overall assessment: exceptionally well-hardened.** Nearly every attacker-controlled length/count is bound-checked against remaining input before allocation, cycle detection is present on every graph/chain walk, recursion is depth-capped or converted to explicit-stack traversal, and integer widenings use `checked`. One genuine memory-amplification issue stands out; the rest are cleared hypotheses.

## Findings

| # | file:line | category | severity | status | finding | recommendation |
|---|---|---|---|---|---|---|
| 1 | `CfbSectorReader.cs:148-170` (`ReadChainToEnd`) & `:255-288` (`ReadMiniChain`) | DoS | Medium | CONFIRMED | Sector-chain cycle detection uses a single global counter `guard++ > _fat.Length` and re-reads looping sectors. A crafted **self-loop / small cycle** (`_fat[5]=5`) is walked ~`_fat.Length` times, each iteration writing `SectorSize` bytes into a growing `MemoryStream` **before** the guard trips. Since `_fat.Length ≤ source.Length/4`, peak intermediate allocation reaches ~**128×** (v3, 512-byte sectors) / ~**1024×** (v4, 4096-byte) the file size; a ~50 MB crafted CFB forces multi-GB allocation → OOM. Reachable from `CompoundFile.Open` (directory read) and any `Materialize`/`OpenStream`. The chain *does* terminate (not an infinite loop); the harm is the pre-termination allocation, and it amplifies **regardless of `CompoundValidationLevel`** (the `MemoryStream` is populated before the guard trips). `ReadMiniChain` has the same shape (~16× via `MiniFatSectorCount`). | Track a **per-chain visited bitset** (each sector may appear at most once in a valid chain) instead of/in addition to the global counter, or cap accumulated bytes at `_source.Length`. Factor a shared visited-set chain walker so both `ReadChainToEnd` and `ReadMiniChain` inherit the bound. |

## Cleared hypotheses (verified safe)

- **CFB FAT/DIFAT build** (`CfbSectorReader.BuildFat`): DIFAT entries deduplicated via `HashSet`, FAT-sector count capped at `source.Length/sectorSize`, DIFAT-chain walk guarded — the documented ~127× amplification / array-size overflow is prevented; the FAT array itself is ~1× input.
- **CFB header** (`CfbHeader.Parse`): `sectorShift ∈ {9,12}` and `miniSectorShift == 6` enforced, so every `stackalloc byte[SectorSize]` is ≤ 4096 — no sector-size stack overflow. Byte-order/signature validated.
- **CFB directory tree** (`CfbDirectory.CollectChildren`): explicit-stack in-order traversal (no native recursion), global `visited[]` bitset rejects sibling/child cycles and shared nodes; out-of-range SIDs pruned by level. `BuildChildren` BFS is also visited-guarded.
- **CFB binary cursor** (`CfbBinaryReader.Take`): every read bounds-checks `_position + count > _data.Length` with a negative-count guard.
- **OLE property sets** (`PropertySetReader`): section count ∈ {1,2}; property/vector counts bounded against remaining bytes before allocation; all string/blob lengths `Ensure`-checked; `characters*2` widenings use `checked`; **VT_VARIANT recursion depth-capped at 32**. Per-property failures isolated by try/catch.
- **BIFF8 payload reads** (`Biff8Payload`): every fixed read validates `offset + width > payload.Length` (and `offset < 0`); `ReadByte` uses unsigned compare.
- **BIFF8 SST** (`Biff8SharedStringTable.Parse`): `unique` count rejected if `> totalBytes`; per-string `charCount` is `ushort` (≤65535) → bounded `StringBuilder`; continuation-boundary and zero-length CONTINUE blocks rejected; a `richRunCount*4+extendedSize` overflow degrades to a no-op skip, not a crash.
- **BIFF8 string decode** (`Biff8StringReader.Decode`): `charCount < 0` / `offset + byteCount > payload.Length` checked before slice.
- **YAML billion-laughs** (`YamlParser.Compose.cs`): `AbsoluteMaxExpandedNodes = 10M` enforced by `EnforceExpansionBudget` with a **memoized explicit-stack post-order walk** — expansion count computed in O(nodes), not exponentially, with `CheckedAdd` throwing early. Alias cycles rejected separately (`DetectAliasCycles`) *before* the count walk, guaranteeing termination.
- **Bencode length-prefix** (`Utf8BencodeReader.cs:619-638`): `999999999:` is rejected (`length > _data.Length - _position` throws); oversized digit runs fail `Utf8Parser.TryParse<int>`; leading-zero lengths rejected. In-memory span → no per-scalar allocation amplification.
- **Depth caps:** Bencode/Toml/YAML all pin `AbsoluteMaxDepth = 64` (non-configurable; clamps any caller override) with the rationale that the ceiling must be hit before native stack exhaustion.
- **Encoding decoders:** every `stackalloc` is size-guarded — Base16 ≤ 256, Bech32 ≤ 128, others fixed. Base58 caps input at `MaxDecodeInputLength = 65536` to bound its O(n²) `BigInteger` accumulation. Output-length widenings are `checked`.
- **Delimited/CSV** (`DelimitedReader`): streaming, `StringBuilder`-based field accumulation is O(n) with no quadratic re-scan; unterminated quoted field throws.

## Hot-path notes

The genuine hot paths — CFB sector-chain traversal, BIFF8 record/SST decode, and the span-based S.T.J readers — are all allocation-conscious (`stackalloc` scratch, `ArrayPool` in the writer, span slicing). Finding #1 is the sole hot path where a small crafted input drives a large allocation; every other length/count reaches an explicit ceiling before `new`.

## Architecture / alignment notes

- CFB cleanly separates the **reader** (attacker-driven: `Cfb*`, `PropertySetReader`) from the **writer** (`CompoundContainerLayout`, driven by a trusted builder tree — out of threat model).
- The BIFF8 record layer is correctly `internal` under `Bodu.Formats.Excel.Biff8`, matching CLAUDE.md.
- Validation-level design (`Minimal` prunes, `Strict` throws) is consistent — but note finding #1 amplifies **regardless of level**, so the fix must not rely on the level.

## Duplication notes

`ReadChainToEnd` and `ReadMiniChain` reimplement the same "walk chain with a length-based guard" loop with the same weakness — fixing #1 should factor a shared visited-set-based chain walker. Bound-checking helpers are otherwise well-centralized (`Biff8Payload`, `PropertySetReader.Ensure`, `CfbBinaryReader.Take`).
