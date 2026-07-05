---
title: Options, results, and eithers
---

# Options, results, and eithers

The `Bodu.Functional` railway primitives make the *shape* of an outcome part of a signature: <xref:Bodu.Functional.Option`1> for a value that may be absent, <xref:Bodu.Functional.Result`1> (and the void <xref:Bodu.Functional.Result>) for an operation that succeeds with a value or fails with a described error, and <xref:Bodu.Functional.Either`2> for a value that is exactly one of two things. All three are `readonly struct`s — no allocation on the happy path — and compose through `Map` / `Bind` / `Match` chains instead of `null` checks and `try`/`catch` control flow.

A few contract points worth keeping in mind:

- **Present values are never `null`.** `Option<T>.Some`, `Result.Success<T>`, and the `Either` factories all reject `null` with <xref:System.ArgumentNullException>. Absence is expressed by the type (`None`, `Failure`, or the other side), never by a smuggled null.
- **The lenient lift is explicit.** Converting a `T?` to an `Option<T>` implicitly maps `null` to `None`; `Option.FromNullable` does the same for nullable value types. Only the strict `Some` factory throws.
- **Each type documents its `default`.** `default(Option<T>)` **is** `None`; `default(Result)` / `default(Result<T>)` **are** failures carrying an empty <xref:Bodu.Functional.ResultError>; `default(Either<,>)` is an explicit *uninitialized* state — neither `IsLeft` nor `IsRight`, and side-requiring operations throw <xref:System.InvalidOperationException> (there is no honest side to default to).
- **`Result<T>` owns the railway bias.** `Either<TLeft,TRight>` is deliberately symmetric — `MapLeft` / `MapRight`, no unqualified `Map` or `Bind` — so a disjoint union is never mistaken for a success/failure pipeline.

## Pattern 1 — replacing null returns with `Option<T>`

```csharp
using Bodu.Functional;

Option<Customer> FindCustomer(string id) =>
    _store.TryGetValue(id, out var customer) ? Option.Some(customer) : Option.None<Customer>();

var greeting = FindCustomer("42")
    .Map(c => c.DisplayName)
    .Filter(name => name.Length > 0)
    .Match(name => $"Hello, {name}!", () => "Hello, guest!");
```

## Pattern 2 — a success/failure pipeline with `Result<T>`

```csharp
using Bodu.Functional;

Result<Order> ParseOrder(string json) =>
    TryParse(json, out var order)
        ? Result.Success(order)
        : Result.Failure<Order>(ResultError.FromMessage("The payload is not a valid order.", code: "ORD001"));

var confirmation = ParseOrder(payload)
    .Bind(Validate)                 // Result<Order> -> Result<Order>
    .Map(o => o.ConfirmationNumber) // failure skips the projection
    .TapError(e => _log.Warn(e.Message))
    .GetValueOrDefault("(none)");
```

`GetValueOrThrow()` converts the failure rail back into an exception at the boundary of railway code: it throws <xref:System.InvalidOperationException> carrying the error's message, with `ResultError.Exception` (when present) attached as `InnerException` — the captured exception is never rethrown directly, so its stack trace stays intact.

## Pattern 3 — a true disjoint union with `Either<TLeft,TRight>`

```csharp
using Bodu.Functional;

Either<StreetAddress, PostOfficeBox> destination = useBox
    ? Either<StreetAddress, PostOfficeBox>.Right(box)
    : Either<StreetAddress, PostOfficeBox>.Left(address);

var label = destination.Match(
    a => a.ToPostalLabel(),
    b => $"PO Box {b.Number}");
```

## Crossing between the types

`Option<T>.ToResult(error)` upgrades absence to a described failure, and `Result<T>.ToOption()` discards the error when only presence matters:

```csharp
var result = FindCustomer("42").ToResult(ResultError.FromMessage("Unknown customer.", code: "CUST404"));
var option = ParseOrder(payload).ToOption();
```

## Async composition

`OptionAsyncExtensions` and `ResultAsyncExtensions` provide Task-based `MapAsync` / `BindAsync` / `MatchAsync` (plus `TapAsync` / `TapErrorAsync` for results) over both `Task<Option<T>>` / `Task<Result<T>>` sources and sync sources with async selectors. Guards throw synchronously at the call site, selectors are never invoked on the empty/failure rail, and every await uses `ConfigureAwait(false)`. The combinators take no <xref:System.Threading.CancellationToken> — they perform no I/O of their own; cancellation belongs inside the caller's delegates.

```csharp
var name = await LoadCustomerAsync(id)      // Task<Option<Customer>>
    .MapAsync(c => c.DisplayName)
    .MatchAsync(n => n, () => "guest");
```

When you need memoization rather than outcome modelling, see [Memoization](memoization.md); for collection-shaped absence (lookups that return collections), the [choosing-a-collection](choosing-a-collection.md) guide covers the dictionary surfaces.
