# Bodu.Numerics.Samples.ComplexNumbers

`Complex<T>` from `Bodu.Numerics` — the generic counterpart of the `double`-only
`System.Numerics.Complex`. Three scenarios cover the arithmetic surface (including the
non-componentwise multiply and the conjugate identity), the polar form and the transcendental
functions, and the reason the type is generic at all.

Every arithmetic and transcendental result is printed beside `System.Numerics.Complex`'s answer for
the same call and marked `match` or `DIFFERS`, so a correct run is verifiable by eye rather than by
trusting the narration.

```bash
dotnet run --project samples/Numerics/Bodu.Numerics.Samples.ComplexNumbers
```

Everything runs offline and deterministically. Formatting uses `CultureInfo.InvariantCulture` so the
output never varies by machine culture.

## Layout

```
  Program.cs                     # runs the three scenarios in order
  SampleConsole.cs               # the what/why/expect banner every scenario opens with
  Scenarios/ComplexBasics.cs     # operators, Conjugate, Reciprocal, Magnitude, Phase
  Scenarios/PolarAndFunctions.cs # FromPolarCoordinates, Sqrt, Exp, Log, Pow, trig
  Scenarios/GenericPrecision.cs  # one generic algorithm over float, double, and Half
```

## Related

- `Bodu.Numerics.Samples.Fractions` — the exact-rational `Fraction<T>`.
- `Bodu.Numerics.Samples.StreamingStatistics` — the single-pass accumulators and `BigDecimal`.
