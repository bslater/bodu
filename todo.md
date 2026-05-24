# Bodu.Core Improvement Plan

## Scope

This plan addresses the recommendations from the static review of the `Bodu.Core` project, ordered by priority. The focus is correctness first, then performance, allocation reduction, API polish, and project cleanup.

---

## P0 — Correctness fixes

### 1. Fix `XorShiftRandom.NextDouble()`

**Problem:**  
`NextDouble()` can currently return exactly `1.0` if the generated `uint` value is `uint.MaxValue`.

**Action:**

- Change scaling from division by `uint.MaxValue` to multiplication by `1.0 / 4294967296.0`.
- Preserve the expected `System.Random` contract: result must be `>= 0.0` and `< 1.0`.

**Suggested tests:**

- Repeatedly call `NextDouble()` and assert values are always `>= 0.0` and `< 1.0`.
- Use deterministic seed paths where possible.
- Add a targeted internal/helper test if needed to simulate `uint.MaxValue`.

---

### 2. Remove modulo bias from bounded random values

**Problem:**  
`Next(int maxValue)` and `Next(int minValue, int maxValue)` use modulo arithmetic, which introduces bias when the range does not divide evenly into the generated integer space.

**Action:**

- Replace modulo-bound generation with rejection sampling.
- Ensure `Next(int maxValue)` handles:
  - `maxValue <= 0`
  - `maxValue == 1`
  - small ranges
  - large ranges

**Suggested tests:**

- `Next(1)` always returns `0`.
- `Next(2)`, `Next(3)`, `Next(10)`, and `Next(int.MaxValue)` stay within range.
- Invalid `maxValue` throws the expected exception.

---

### 3. Fix overflow risk in `XorShiftRandom.Next(int minValue, int maxValue)`

**Problem:**  
`maxValue - minValue` can overflow for large ranges, especially when `minValue` is negative.

**Action:**

- Compute the range using `long` or `ulong`.
- Add a bounded `ulong` helper if required.
- Ensure `minValue == maxValue` returns `minValue`.
- Ensure `minValue > maxValue` throws.

**Suggested tests:**

- `Next(int.MinValue, int.MaxValue)` does not overflow.
- `Next(-10, 10)` stays within range.
- `Next(5, 5)` returns `5`.
- `Next(10, 5)` throws.

---

## P1 — Safety and high-value performance improvements

### 4. Harden `PooledBufferBuilder<T>` capacity growth

**Problem:**  
Calls such as `_count + source.Length` or `_count + count` can overflow before the growth logic runs.

**Action:**

- Use checked arithmetic before calling growth methods.
- Ensure overflow produces a clear, expected exception.
- Review all append/add/range paths for unchecked additions.

**Suggested tests:**

- Add overflow-path tests using controlled/internal hooks where practical.
- Verify normal append/range operations still grow correctly.
- Verify destination sizing and copy behavior remain unchanged.

---

### 5. Verify reference clearing in `PooledBufferBuilder<T>`

**Problem:**  
The builder should not retain references after `Reset()` or `Dispose()` when `T` contains references.

**Action:**

- Confirm only the written range is cleared.
- Avoid clearing full rented capacity unless explicitly required.
- Ensure disposed builders do not expose stale memory.

**Suggested tests:**

- Use reference objects and `WeakReference`.
- Append references, call `Reset()`, force GC, and verify references are collectible.
- Repeat for `Dispose()`.
- Verify value-type builders do not perform unnecessary clearing.

---

### 6. Add convenience APIs to `PooledBufferBuilder<T>`

**Action:**

Add:

```csharp
public bool TryCopyTo(Span<T> destination);
public T[] ToArrayAndDispose();
```

**Rationale:**

- `TryCopyTo` avoids exception-driven control flow.
- `ToArrayAndDispose` supports the common build-materialize-release pattern.

**Suggested tests:**

- `TryCopyTo` returns `false` when the destination is too small.
- `TryCopyTo` returns `true` and copies values when the destination is large enough.
- `ToArrayAndDispose` returns the written values and disposes the builder.
- Access after disposal throws expected exceptions.

---

### 7. Add faster `AppendRange(IEnumerable<T>)` paths

**Problem:**  
Enumerable-based append paths may enumerate when the source is already countable or span-backed.

**Action:**

Add fast paths for:

- `T[]`
- `List<T>` using `CollectionsMarshal.AsSpan`
- `ICollection<T>`
- `IReadOnlyCollection<T>`

**Suggested tests:**

- Append from array.
- Append from list.
- Append from collection.
- Append from read-only collection.
- Append from lazy enumerable.
- Confirm order is preserved.

---

### 8. Add cheaper count/empty APIs to `ConcurrentHashSet<T>`

**Problem:**  
`Count` and strict `IsEmpty` semantics require acquiring all locks.

**Action:**

- Keep the current strict `Count` behavior if snapshot semantics are desired.
- Add a non-blocking approximate count property.
- Consider making `IsEmpty` a cheap volatile-read check, or add a separate `IsEmptySnapshot` property.

**Candidate APIs:**

```csharp
public int ApproximateCount { get; }
public bool IsEmptySnapshot { get; }
```

**Suggested tests:**

- Approximate count returns expected value without concurrency.
- Snapshot count remains exact.
- Concurrent add/remove stress tests do not throw.
- API documentation clearly explains snapshot versus approximate behavior.

---

### 9. Validate `ConcurrentHashSet<T>` internal concurrency level

**Problem:**  
The internal constructor should guard invalid `concurrencyLevel` values.

**Action:**

- Throw when `concurrencyLevel <= 0`.
- Add explicit tests for invalid concurrency levels.

**Suggested tests:**

- `concurrencyLevel == 0` throws.
- `concurrencyLevel < 0` throws.
- Valid concurrency levels construct successfully.

---

### 10. Reconsider default `ConcurrentHashSet<T>` concurrency level

**Problem:**  
Using `Environment.ProcessorCount` can allocate many locks on high-core machines for small sets.

**Action:**

- Consider clamping the default concurrency level.
- Suggested default:

```csharp
Math.Clamp(Environment.ProcessorCount, 1, 32)
```

**Suggested tests:**

- Constructor succeeds across default paths.
- Internal lock count is sane via internal test access if available.
- Existing behavior-dependent tests are updated if required.

---

## P2 — Allocation reduction and API refinement

### 11. Make `WeekPattern` a readonly value type

**Problem:**  
`WeekPattern` is documented as immutable but is not declared as a `readonly struct`.

**Action:**

- Change to:

```csharp
public readonly partial struct WeekPattern
```

- Make backing fields readonly where possible.

**Suggested tests:**

- Existing behavioral tests should continue to pass.
- Verify `With` and `Without` return new values.
- Verify original values are unchanged.

---

### 12. Add allocation-free `WeekPattern` enumeration

**Problem:**  
A `yield return` enumerator may allocate when enumerated through interfaces.

**Action:**

- Add a custom struct enumerator for direct `foreach`.
- Keep explicit `IEnumerable<DayOfWeek>` implementation for compatibility.

**Suggested tests:**

- Enumeration order is stable.
- Empty pattern enumerates no days.
- Full pattern enumerates all seven days.
- Interface enumeration remains correct.

---

### 13. Review `Batch` extension ownership semantics

**Problem:**  
Batching APIs can accidentally expose reused mutable buffers or allocate more than necessary.

**Action:**

- Confirm whether each yielded batch is independently owned.
- Document ownership semantics.
- Prefer returning arrays or immutable/read-only batch views.

**Suggested tests:**

- Modifying one returned batch does not affect another.
- Final partial batch is correct.
- Empty input produces no batches.
- Invalid batch size throws.

---

### 14. Review `Randomize` extension implementation

**Problem:**  
If implemented with `OrderBy(_ => random.Next())`, it is slower and less predictable than Fisher-Yates.

**Action:**

- Use Fisher-Yates shuffle over a materialized array/list.
- Ensure random source uses corrected bounded random generation.

**Suggested tests:**

- Output contains the same items as input.
- Original source is not mutated unless explicitly documented.
- Empty and single-item sequences work.
- Deterministic random source produces deterministic order.

---

### 15. Review eager/lazy behavior in enumerable extensions

**Action:**

Review and document eager versus lazy behavior for:

- `ForEach`
- `Cache`
- `RecursiveSelect`
- `Batch`
- `Randomize`

**Suggested tests:**

- Confirm each method enumerates the source the expected number of times.
- Confirm lazy methods do not enumerate before iteration.
- Confirm eager methods document immediate enumeration.

---

## P3 — Project cleanup and maintainability

### 16. Simplify target framework conditionals

**Problem:**  
The project currently targets `net8.0`, but still contains conditional exclusions for older target frameworks.

**Action:**

Choose one direction:

#### Option A — Stay `net8.0+`

- Remove stale `netstandard2.0` and `netstandard2.1` conditional exclusions.
- Simplify project file maintenance.

#### Option B — Restore multi-targeting

- Change to a real multi-target setup.
- Validate all conditional files compile for each target.
- Add CI coverage for all target frameworks.

**Recommendation:**  
Use Option A unless there is a real consumer requirement for `netstandard`.

---

### 17. Review `InternalsVisibleTo`

**Problem:**  
The Core project exposes internals to tests and multiple sibling libraries.

**Action:**

- Confirm every friend assembly still needs internal access.
- Remove unused internal exposure.
- Prefer public abstractions or shared internal source where appropriate.

**Suggested tests/checks:**

- Remove one friend assembly at a time and build.
- Keep only required entries.

---

### 18. Replace placeholder package/company metadata

**Problem:**  
Some files or project metadata may still use placeholder company/copyright values.

**Action:**

- Standardize package metadata.
- Standardize source headers.
- Ensure generated NuGet metadata is correct.

---

## Suggested implementation order

1. Fix `XorShiftRandom.NextDouble()`.
2. Replace modulo-bounded random generation with rejection sampling.
3. Fix `XorShiftRandom.Next(minValue, maxValue)` overflow behavior.
4. Harden `PooledBufferBuilder<T>` checked growth paths.
5. Add `PooledBufferBuilder<T>` reference-clearing tests.
6. Add `TryCopyTo` and `ToArrayAndDispose`.
7. Add `AppendRange(IEnumerable<T>)` fast paths.
8. Add `ConcurrentHashSet<T>.ApproximateCount`.
9. Add or clarify `ConcurrentHashSet<T>` snapshot versus approximate empty semantics.
10. Validate internal `ConcurrentHashSet<T>` concurrency level.
11. Reconsider default `ConcurrentHashSet<T>` concurrency level.
12. Convert `WeekPattern` to `readonly partial struct`.
13. Add allocation-free `WeekPattern` enumerator.
14. Review and harden `Batch`.
15. Review and harden `Randomize`.
16. Review eager/lazy behavior across enumerable extensions.
17. Simplify target framework conditionals.
18. Review `InternalsVisibleTo`.
19. Standardize package and copyright metadata.

---

## Definition of done

The work is complete when:

- All existing tests pass.
- New tests cover the listed correctness and edge cases.
- Random bounded generation no longer uses modulo bias.
- `NextDouble()` never returns `1.0`.
- `PooledBufferBuilder<T>` has checked capacity growth and reference-clearing coverage.
- `ConcurrentHashSet<T>` clearly distinguishes exact snapshot operations from approximate non-blocking operations.
- `WeekPattern` immutability is compiler-enforced.
- Extension method eager/lazy and ownership semantics are documented and tested.
- Project-file conditionals and friend assemblies are intentionally retained or removed.