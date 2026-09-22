# Bodu.Globalization.Calendar.Samples.Plugin.Contoso

The plugin assembly loaded by `Bodu.Globalization.Calendar.Samples.Plugins`. This is a **class
library, not a console app** — there is nothing to run here; build it and run the host.

It exists as its own project so the host has a genuinely external assembly to discover, rather than a
type it could have referenced directly. It depends only on the contract packages
(`Bodu.Globalization.Calendar` for `INotableDateAlgorithm`, `Bodu.Globalization.Calendar.Plugins` for
the plugin interface and the assembly attribute) and has no reference back to the host.

## What a plugin assembly needs

1. An assembly-level `[assembly: NotableDatePlugin(typeof(T))]` attribute naming the entry point.
   This is what the loader looks for, so there is no naming convention to follow, no manifest file,
   and no reflection over every type in the assembly.
2. A type implementing `INotableDateAlgorithmPlugin` with a `Name`, a `Version`, and
   `GetAlgorithms()` returning key/algorithm pairs.
3. One or more `INotableDateAlgorithm` implementations. Each answers a single question — *what date
   in this year?* — and returns `null` for years it does not apply to.

The key (`contoso.founding-day`) is the entire contract between this assembly and a rule document:
the document references the string, the registry resolves it to the instance, and neither side needs
a type from the other. Keys are namespaced by convention so two plugins cannot collide.

## Layout

```
  ContosoCalendarPlugin.cs   # the assembly attribute and the plugin entry point
  FoundingDayAlgorithm.cs    # the contributed algorithm
```
