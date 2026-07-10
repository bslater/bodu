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

```
AU 2024 non-working notable dates: 8
Payday falling on Anzac Day pays on: 2024-04-24 (Wednesday)
```

The factory overload (`AddNotableDateService(sp => ...)`) defers resource loading to first
resolution — noted in the code for hosts that want lazy startup.

**APIs demonstrated.** `AddNotableDateService(resource)`,
`AsiaPacificCalendarData.LoadResource`, container resolution of `INotableDateService`,
`SnapToWorkingDayBackward` over an injected service.

### ReloadableResource (`Scenarios/ReloadableResource.cs`)

**Intent.** Rule data changes — a legislated new holiday, a tenant switch, a rules refresh job.
The reloadable registration swaps the data underneath live consumers: they keep their injected
`INotableDateService` reference; only the resource moves.

**What it does.** Registers `AddReloadableNotableDateService` with the AU pack, resolves and
*holds* the service, queries it, then resolves `MutableNotableDateResourceProvider` (the
operations-side handle) and calls `Reload` with the NZ pack — and queries the same held
reference again.

**What to expect.**

```
Initial resource : AU (22 notable dates in 2024)
After Reload     : NZ (19 notable dates in 2024)
2024-02-06 in NZ : Waitangi Day
```

The same service instance answers NZ questions after the swap — Waitangi Day resolving is the
proof the new data is live. No re-resolution, no restart, no consumer code change.

**APIs demonstrated.** `AddReloadableNotableDateService(initialResource)`,
`MutableNotableDateResourceProvider.Reload`, the held-reference reload semantics.

## NuGet equivalent

```bash
dotnet add package Bodu.Globalization.Calendar.DependencyInjection
dotnet add package Bodu.Globalization.Calendar.AsiaPacific
```
