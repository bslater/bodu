# Bodu.Globalization.Calendar.Samples.RegionalData

The five regional data packs — `Bodu.Globalization.Calendar.Americas`, `.AsiaPacific`, `.Europe`,
`.MiddleEast`, and `.Africa`. Two scenarios: what each pack covers, and why a notable-date lookup is
always scoped to a territory rather than asked globally.

The second scenario is the one worth reading. One calendar date means different things in different
places — 25 December is a public holiday in four of the five sample territories and not one in `AE`
at all — which is why there is no global "is it a holiday" call to make. It closes with AU's 2027
weekend substitutions, where `IsObserved` marks the shifted day and `ActualDate` keeps the date it
shifted from, so the adjustment is auditable rather than implied.

```bash
dotnet run --project samples/Globalization.Calendar/Bodu.Globalization.Calendar.Samples.RegionalData
```

Each pack embeds its own rules, so every scenario runs offline and deterministically — no data
directory to deploy, no download on first use.

## Layout

```
  Program.cs                           # runs the two scenarios in order
  SampleConsole.cs                     # the what/why/expect banner every scenario opens with
  Scenarios/RegionCoverage.cs          # supported territories and a resolved year per pack
  Scenarios/SameDayAcrossRegions.cs    # one date across five territories, plus substitutions
```

## Related

- `Bodu.Globalization.Calendar.Samples.NotableDatesBasics` — the resolution surface in depth.
- `Bodu.Globalization.Calendar.Samples.WorkingDays` — working-day arithmetic over the same data.
