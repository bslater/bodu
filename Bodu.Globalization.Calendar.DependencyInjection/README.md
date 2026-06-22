# Bodu.Globalization.Calendar.DependencyInjection

`Microsoft.Extensions.DependencyInjection` integration for `Bodu.Globalization.Calendar`. Registers an `INotableDateService` over a supplied `NotableDateResource` so the calendar engine can be injected into application services.

## Installation

```shell
dotnet add package Bodu.Globalization.Calendar.DependencyInjection
```

Targets `net8.0`. Extension methods extend `IServiceCollection` in the `Bodu.Globalization.Calendar` namespace.

## Registration

```csharp
using Bodu.Globalization.Calendar;

services.AddNotableDateService(AmericasCalendarData.LoadResource("US"));
// or build the resource lazily from the provider:
services.AddNotableDateService(sp => /* resolve a NotableDateResource */);
```

| Method | Purpose |
|---|---|
| `AddNotableDateService(NotableDateResource)` | Register `INotableDateService` as a singleton over a fixed resource |
| `AddNotableDateService(Func<IServiceProvider, NotableDateResource>)` | Register via a factory resolved from the container |
| `AddReloadableNotableDateService(NotableDateResource)` | Register a reloadable service backed by a mutable resource provider |

Because the resource is immutable and the resolver is stateless, all registrations use the singleton lifetime.

## Testing

```bash
dotnet test Bodu.Globalization.Calendar.DependencyInjection/test/Bodu.Globalization.Calendar.DependencyInjection.Test.csproj --settings bvt.runsettings
```

## License

MIT. © Bodu Pty. Ltd.
