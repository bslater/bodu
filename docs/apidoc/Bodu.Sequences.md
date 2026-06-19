---
uid: Bodu.Sequences
---

![Bodu.Core](~/images/hero-core.svg)

## Purpose

**Bodu.Sequences** is the home of `SequenceGenerator` — lazily evaluated `IEnumerable<T>` *producers* that mirror the conventions of `System.Linq.Enumerable` and complement it with sequence shapes the BCL does not provide directly. Reach for it when you need a counted or descending range, a bounded or unbounded repeat, a state-machine-style generator, or one of several well-known mathematical series, all without materializing an intermediate collection.

Unlike the `Bodu.Collections.*` namespaces — which hold container data structures and the extension methods that operate on existing sequences — this namespace holds only the static factories that *create* sequences from scratch.

## Key types

- <xref:Bodu.Sequences.SequenceGenerator> — the single static factory class.
  - **General-purpose primitives** — `Range` (inclusive start/stop with inferred or explicit step over `int`, plus a count-bounded `long` overload), `Repeat` (finite or unbounded single-value feed), `NextWhile` (a state-machine generator driven by a seed, a predicate, and a successor function — value, indexed, and custom-state overloads), and `Factory` (adapts a delegate-returned `IEnumerator<T>` into a re-enumerable `IEnumerable<T>`).
  - **Named mathematical series** — `Fibonacci`, `Farey`, `Leibniz`, `LookAndSay`, `ThueMorse`.

## Example

```csharp
using Bodu.Sequences;

// A counted descending range.
foreach (int n in SequenceGenerator.Range(start: 10, stop: 0, step: -2))
    Console.WriteLine(n); // 10, 8, 6, 4, 2

// Fibonacci numbers within a value window (not a fixed count).
long[] fibs = SequenceGenerator.Fibonacci(min: 0, max: 100).ToArray();

// A stateful generator — powers of two while they stay positive.
IEnumerable<int> powers = SequenceGenerator.NextWhile(
    initialValue: 1,
    conditionHandler: v => v > 0,
    resultSelector: v => v * 2);
```

## Notes

- **Deferred and allocation-light.** Every member returns a deferred sequence; nothing is produced until the consumer iterates, and allocation is limited to the iterator state.
- **Single-pass side effects.** Re-enumerating a returned sequence re-invokes the supplied delegates. Materialise with `ToArray()` / `ToList()` for a stable snapshot.
- **Unbounded shapes exist.** `Repeat(value)`, `Range(start, stop, step: 0)`, and a never-failing `NextWhile` predicate produce infinite sequences — bound them with `Take` / `TakeWhile`.
- **Argument validation.** Public factories validate their arguments via <xref:Bodu.ThrowHelper>.
- **See also:** the [Bodu.Core introduction](~/docs/core/index.md), the <xref:Bodu.Collections.Generic> container types, and the <xref:Bodu.Collections.Generic.Extensions> sequence-shaping extension methods.
