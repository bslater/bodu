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

if (failures == 0)
{
    Console.WriteLine("Bodu.Numerics AOT smoke: all checks passed.");
    return 0;
}

Console.Error.WriteLine($"Bodu.Numerics AOT smoke: {failures} check(s) failed.");
return 1;
