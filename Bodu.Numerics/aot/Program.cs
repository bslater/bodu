// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Bodu.Numerics;

int failures = 0;

void Check(bool condition, string name)
{
    if (condition)
    {
        Console.WriteLine($"ok   : {name}");
    }
    else
    {
        Console.Error.WriteLine($"FAIL : {name}");
        failures++;
    }
}

// Bounded backing types resolve their range through the reflection-based bounds probe in Fraction<T>'s static ctor.
Check(Fraction<int>.MinValue == new Fraction<int>(int.MinValue), "Fraction<int>.MinValue");
Check(Fraction<int>.MaxValue == new Fraction<int>(int.MaxValue), "Fraction<int>.MaxValue");
Check(Fraction<long>.MaxValue == new Fraction<long>(long.MaxValue), "Fraction<long>.MaxValue");

// An unbounded backing type has no IMinMaxValue<T>, so the probe must report "unbounded" and MinValue must throw.
bool unboundedThrew = false;
try
{
    _ = Fraction<BigInteger>.MinValue;
}
catch (NotSupportedException)
{
    unboundedThrew = true;
}

Check(unboundedThrew, "Fraction<BigInteger>.MinValue throws NotSupportedException");

// The non-throwing narrowing check must reject a value that cannot fit the fixed-width backing type.
Check(!Fraction<int>.TryCreate(int.MinValue, -1, out _), "Fraction<int>.TryCreate overflow returns false");

// A basic arithmetic + interval round-trip, to exercise the generic-math and interval paths under AOT.
Check((new Fraction<int>(1, 3) + new Fraction<int>(1, 6)) == new Fraction<int>(1, 2), "Fraction<int> arithmetic");
Check(Interval<int>.Closed(1, 5).Contains(3), "Interval<int>.Contains");

// BigDecimal exercises its generic-math conversion factories (TryConvertFrom/To use TOther.Create*), which must
// resolve without MakeGenericMethod under AOT. Exact arithmetic, precision-controlled division, and parsing too.
Check(BigDecimal.Add(0.1m, 0.2m) == BigDecimal.FromDecimal(0.3m), "BigDecimal.Add exact");
Check(BigDecimal.Divide(10m, 3m, 2, MidpointRounding.ToEven) == BigDecimal.FromDecimal(3.33m), "BigDecimal.Divide scaled");
Check(CreateChecked<BigDecimal, int>(5) == BigDecimal.FromDecimal(5m), "BigDecimal.CreateChecked");
Check(int.CreateTruncating(BigDecimal.Parse("3.5", System.Globalization.CultureInfo.InvariantCulture)) == 3, "BigDecimal int.CreateTruncating");

// The statistics accumulators widen samples with double.CreateChecked over a generic T and index [InlineArray]
// storage, both of which must resolve without reflection under AOT.
var stats = new RunningStatistics<int>();
foreach (var sample in new[] { 1, 2, 3, 4 })
    stats.Add(sample);

Check(stats.Mean == 2.5 && stats.Minimum == 1 && stats.Maximum == 4, "RunningStatistics<int> moments");

var median = RunningQuantile<double>.CreateMedian();
foreach (var observation in new[]
{
    0.02, 0.15, 0.74, 3.39, 0.83, 22.37, 10.15, 15.43, 38.62, 15.92,
    34.60, 10.28, 1.47, 0.40, 0.05, 11.39, 0.27, 0.42, 0.09, 11.37,
})
{
    median.Add(observation);
}

Check(Math.Abs(median.Estimate - 4.44063) < 1e-4, "RunningQuantile<double> P² estimate");

var movingSum = new MovingSum<decimal>(3);
foreach (var sample in new[] { 1.5m, 2.5m, 3.5m, 4.5m })
    movingSum.Add(sample);

Check(movingSum.Sum == 10.5m, "MovingSum<decimal> window sum");

var movingMinMax = new MovingMinMax<int>(3);
foreach (var sample in new[] { 5, 1, 4, 2 })
    movingMinMax.Add(sample);

Check(movingMinMax.Minimum == 1 && movingMinMax.Maximum == 4, "MovingMinMax<int> window extrema");

if (failures == 0)
{
    Console.WriteLine("Bodu.Numerics AOT smoke: all checks passed.");
    return 0;
}

Console.Error.WriteLine($"Bodu.Numerics AOT smoke: {failures} check(s) failed.");
return 1;

// Reaches the INumberBase<T>.CreateChecked static-abstract member through a generic constraint (it is an explicit
// interface implementation, so it is not callable off the concrete type name).
static T CreateChecked<T, TSource>(TSource value)
    where T : INumberBase<T>
    where TSource : INumberBase<TSource> =>
    T.CreateChecked(value);
