---
title: Classic string hashes
---

# Classic string hashes

A collection of the classic "one-liner" hash functions that appear in textbooks, compilers, and early web servers. They all produce a 32-bit or 64-bit output, none of them takes a key, and their public API is identical — configure any optional seed, call `Append` to feed data, and call `GetCurrentHash` to read the digest.

**None of these are cryptographic.** They are in the package for interoperability with legacy systems, for teaching purposes, and for use inside a trust boundary where collision-DoS is not a concern. For anything adversary-facing, use <xref:Bodu.Security.Cryptography.SipHash64>; see the [cryptography hashing guide](../cryptography/hashing.md).

## The family at a glance

| Type | Width | Seed / configuration | Origin |
|---|---|---|---|
| <xref:Bodu.IO.Hashing.Bernstein> | 32 bits | `InitialValue` (default `5381`), `UseModifiedAlgorithm` (xor vs add) | Daniel J. Bernstein's "djb2", posted to `comp.lang.c`. |
| <xref:Bodu.IO.Hashing.BKDR> | 32 bits | `Seed` — one of a published set of odd multipliers | Kernighan & Ritchie, *The C Programming Language*. |
| <xref:Bodu.IO.Hashing.SDBM> | 32 bits | None | The SDBM public-domain database. |
| <xref:Bodu.IO.Hashing.JSHash> | 32 bits | None (seed `0x4E67C6A7`) | Justin Sobel's JavaScript-origin hash. |
| <xref:Bodu.IO.Hashing.Elf64> | 64 bits | `Seed` (default `0`) | The ELF symbol-table hash, widened to 64 bits. |
| <xref:Bodu.IO.Hashing.ApHash> | 32 bits | None (seed `0xAAAAAAAA`) | Arash Partow's hash. |
| <xref:Bodu.IO.Hashing.Pjw32> | 32 bits | None | Peter Weinberger's PJW hash (AT&T compiler). |

All seven derive from <xref:System.IO.Hashing.NonCryptographicHashAlgorithm?displayProperty=nameWithType>.

## Pattern 1 — a default-configured hash

Every type has a parameterless constructor that uses the historically canonical parameters for that function:

```csharp
using System.Text;
using Bodu.IO.Hashing;

byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");

using var hash = new Bernstein();             // djb2 — seed 5381, XOR form
hash.Append(data);
byte[] digest = hash.GetCurrentHash();
uint h = BitConverter.ToUInt32(digest);
```

Swap `Bernstein` for any of the others — the API is the same.

## Pattern 2 — Bernstein (djb2), add vs XOR

Bernstein's original posting used addition (`h = h * 33 + c`). The XOR form (`h = h * 33 ^ c`) distributes slightly better on ASCII input and is what most later ports use. `Bernstein` exposes both through `UseModifiedAlgorithm`.

```csharp
using Bodu.IO.Hashing;

// Original: h = (h * 33) + c
using var original = new Bernstein(Bernstein.DefaultInitialValue, useModifiedAlgorithm: false);

// "djb2a":  h = (h * 33) ^ c — the common modern variant
using var modified = new Bernstein(Bernstein.DefaultInitialValue, useModifiedAlgorithm: true);

// Or set the properties before the first Append
using var alt = new Bernstein { InitialValue = 0, UseModifiedAlgorithm = true };
```

Both properties are only settable **before** the first `Append` — changing them mid-stream would invalidate the running state, so the setters throw once input has been fed.

## Pattern 3 — BKDR's published multiplier set

BKDR is a family: the multiplier is a repeating-digit odd number from the published set (`31`, `131`, `1313`, `13131`, `131313`, `1313131`, `13131313`, `131313131`, `1313131313`). The default is `BKDR.DefaultSeed` (`131`):

```csharp
using Bodu.IO.Hashing;

using var bkdr = new BKDR(seed: 1313);         // must be one of the published values
bkdr.Append(Encoding.UTF8.GetBytes("example"));
byte[] digest = bkdr.GetCurrentHash();
```

Passing a value outside the published set throws — the seed is a property of the published "standard BKDR family", not an arbitrary multiplier.

## Pattern 4 — Elf64 with a custom seed

`Elf64` widens the classic ELF symbol-table hash to 64 bits and exposes a seed so you can salt it for separate hash-table lanes:

```csharp
using Bodu.IO.Hashing;

using var elf = new Elf64(seed: 0xDEADBEEFUL);
elf.Append(Encoding.UTF8.GetBytes("/usr/bin/ls"));
byte[] digest = elf.GetCurrentHash();
```

The seed is **not a key** — it does not provide adversarial resistance. It only re-origins the accumulator so two parallel tables get independent distributions.

## Pattern 5 — `Append` / `GetCurrentHash` / `Reset`

Every type in this family behaves identically under the `NonCryptographicHashAlgorithm` contract:

```csharp
using Bodu.IO.Hashing;

using var hash = new SDBM();

hash.Append(header);
hash.Append(body);
byte[] partial = hash.GetCurrentHash();    // snapshot, non-destructive
hash.Append(trailer);
byte[] full = hash.GetCurrentHash();

hash.Reset();                              // back to the configured seed / initial state
```

## Picking a function

- **Hashing identifiers inside a compiler or symbol table?** `Pjw32` or `Elf64` — these are the functions you'll meet in the original source.
- **Quick hash-table function in throwaway code?** `Bernstein` with `UseModifiedAlgorithm = true` is simple, well-known, and distributes reasonably.
- **Hashing user-controlled input?** None of these — reach for <xref:Bodu.Security.Cryptography.SipHash64>.
- **Reproducing a known published digest from another tool?** Match on algorithm, width, seed, and (for Bernstein) the add-vs-XOR variant.

For general-purpose fingerprinting where quality and speed both matter, **`CityHash64`** and **`Fnv1a64`** outperform everything in this family — see the [CityHash](cityhash.md) and [FNV](fnv.md) guides.

## Where to go next

- [Using FNV](fnv.md), [Using CityHash](cityhash.md) — modern non-cryptographic hashes with better distribution.
- [Using Pearson](pearson.md) — table-driven classic hash with configurable output width.
- [Cryptography hashing guide](../cryptography/hashing.md) — when a classic hash is not enough.
- [Bodu.IO.Hashing namespace page](../../apidoc/Bodu.IO.Hashing.md) — key types and design notes.
