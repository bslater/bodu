# Bodu.Globalization.Calendar.Samples.ServiceHosting

Hosting the notable-date service in a dependency-injection container: the simple singleton
registration, and the reloadable registration whose rule data can be swapped at run time without
restarting the host or breaking held service references.

```bash
dotnet run --project samples/Globalization.Calendar/Bodu.Globalization.Calendar.Samples.ServiceHosting
```

## Scenarios

### BasicRegistration (`Scenarios/BasicRegistration.cs`)

**Intent.** The composition-root norm: load the resource once, register an immutable
`INotableDateService` singleton, and let consumers take it by constructor injection — including
the working-day extensions, which accept the service as a parameter.

**What it does.** Registers `AsiaPacificCalendarData.LoadResource("AU")` with
`AddNotableDateService`, resolves `INotableDateService` from the container, counts the 2024
non-working dates, and answers a payroll question ("payday fell on Anzac Day — when do we
actually pay?") with `SnapToWorkingDayBackward`.

**What to expect.**

```text
  AU 2024 non-working notable dates: 8  (resolved by interface - the resource was parsed once at composition, not per query)
  Payday falling on Anzac Day pays on: 2024-04-24 (Wednesday)  (snapped backward, not forward - for a pay run, late is a different kind of wrong from early)
```

The factory overload (`AddNotableDateService(sp => ...)`) defers resource loading to first
resolution — noted in the code for hosts that want lazy startup.

**APIs demonstrated.** `AddNotableDateService(resource)`,
`AsiaPacificCalendarData.LoadResource`, container resolution of `INotableDateService`,
`SnapToWorkingDayBackward` over an injected service.

### KeyedRegistration (`Scenarios/KeyedRegistration.cs`)

**Intent.** A multi-tenant process needs several calendars at once. Show keyed registration
keeping tenant awareness in the composition root, the options overload composing collaborators,
and the `TryAdd` semantics that make registration order stop mattering.

**What it does.** Registers one service per jurisdiction under a key, registers an unkeyed
service composed with a custom `NotableDateAlgorithmRegistry`, resolves two services by key, and
re-registers an existing key with different data.

**What to expect.**

```text
  AU keyed service: 1 occurrence(s) on Anzac Day  (resolved by key, so tenant awareness stays in the composition root rather than in every consumer)
  NZ keyed service: 1 occurrence(s) on Waitangi Day  (a different jurisdiction in the same process, with no shared state between them)
  Second AU registration ignored: first registration wins.  (TryAdd semantics - a library registering defaults and an application overriding them cannot clobber each other by ordering)
```

The alternative to keys is a factory every date-aware class has to know about, which pushes
tenant awareness through the whole codebase. Re-registering an existing key is a no-op rather
than a replacement, so a library registering its defaults and an application overriding them
cannot clobber each other by ordering.

**APIs demonstrated.** `AddNotableDateService(key, resource)`,
`GetRequiredKeyedService<INotableDateService>`, `NotableDateServiceOptions.Algorithms`,
`NotableDateAlgorithmRegistry.Register`, `INotableDateAlgorithm`.

### ReloadableResource (`Scenarios/ReloadableResource.cs`)

**Intent.** Rule data changes — a legislated new holiday, a tenant switch, a rules refresh job.
The reloadable registration swaps the data underneath live consumers: they keep their injected
`INotableDateService` reference; only the resource moves.

**What it does.** Registers `AddReloadableNotableDateService` with the AU pack, resolves and
*holds* the service, queries it, then resolves `MutableNotableDateResourceProvider` (the
operations-side handle) and calls `Reload` with the NZ pack — and queries the same held
reference again.

**What to expect.**

```text
  Initial resource : AU (22 notable dates in 2024)
  After Reload     : NZ (19 notable dates in 2024)  (the same reference the consumer captured at construction - no re-resolution and no restart)
  2024-02-06 in NZ : Waitangi Day  (a question the Australian pack could not have answered - the data really was replaced)
```

The same service instance answers NZ questions after the swap — Waitangi Day resolving is the
proof the new data is live. No re-resolution, no restart, no consumer code change.

**APIs demonstrated.** `AddReloadableNotableDateService(initialResource)`,
`MutableNotableDateResourceProvider.Reload`, the held-reference reload semantics.

## Layout

```text
Bodu.Globalization.Calendar.Samples.ServiceHosting/
  Program.cs                          # runs the scenarios in order
  SampleConsole.cs                    # the What / Why / Expect scenario banner
  Scenarios/BasicRegistration.cs
  Scenarios/KeyedRegistration.cs
  Scenarios/ReloadableResource.cs
```

## NuGet equivalent

```bash
dotnet add package Bodu.Globalization.Calendar.DependencyInjection
dotnet add package Bodu.Globalization.Calendar.AsiaPacific
```
