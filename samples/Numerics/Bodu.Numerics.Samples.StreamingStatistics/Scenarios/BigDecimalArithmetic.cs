// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BigDecimalArithmetic.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using Bodu.Numerics;

namespace Bodu.Numerics.Samples.StreamingStatistics.Scenarios;

/// <summary>
/// Demonstrates <see cref="BigDecimal" />: an arbitrary-precision decimal built from an unscaled
/// <see cref="BigInteger" /> and a base-10 scale. Arithmetic is exact and preserves scale; division
/// and rounding are explicit about the scale and midpoint mode they use.
/// </summary>
public static class BigDecimalArithmetic
{
    /// <summary>
    /// Parses, adds, multiplies, divides, and rounds big decimals, printing each exact result.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "BigDecimal - exact scaled arithmetic",
            what: "Adds tenths, constructs a value from an unscaled integer and a scale, multiplies, divides to a " +
                  "requested precision, and rounds under two midpoint modes.",
            why: "Money is the case that forces this. A double cannot hold 0.10, so cents drift and a ledger " +
                 "stops balancing; decimal fixes the base but caps at 28 digits and a fixed scale. BigDecimal " +
                 "carries an arbitrary-precision unscaled value with an explicit scale, so it is exact at any " +
                 "magnitude - and because division cannot always be exact, it requires the caller to state the " +
                 "precision rather than silently truncating. The midpoint modes matter for the same reason: " +
                 "banker's rounding exists so a long run of .5 values does not accumulate an upward bias.",
            expect: "0.10 + 0.20 is exactly 0.3 at scale 1. Multiplication adds scales, so 123.45 * 1.10 has " +
                    "scale 3 rather than being trimmed. The same 2.5 rounds to 2 under ToEven and 3 under " +
                    "AwayFromZero - one value, two defensible answers, which is why the mode is explicit.");

        // Parse preserves the written scale: "0.10" keeps two fractional digits.
        var a = BigDecimal.Parse("0.10", CultureInfo.InvariantCulture);
        var b = BigDecimal.Parse("0.20", CultureInfo.InvariantCulture);

        // Exact addition - no binary floating-point surprise: 0.10 + 0.20 is exactly 0.3
        // (the redundant trailing zero is trimmed, leaving scale 1).
        var sum = a + b;
        Console.WriteLine($"  0.10 + 0.20             : {sum} (scale {sum.Scale})  (exactly 0.3 - the sum that famously is not 0.3 in double, and the reason a ledger drifts)");

        // A value can also be built directly from an unscaled integer and a scale: 12345 x 10^-2.
        var price = new BigDecimal(new BigInteger(12345), scale: 2);   // 123.45
        Console.WriteLine($"  unscaled 12345, scale 2 : {price} (precision {price.Precision})  (value and scale are stored separately, so 123.45 is exact rather than nearest-representable)");

        // Multiplication adds the scales of its operands: the 2-place 123.45 times "1.10"
        // (which normalizes to the 1-place 1.1) yields a 3-place product.
        var product = price * BigDecimal.Parse("1.10", CultureInfo.InvariantCulture);
        Console.WriteLine($"  123.45 * 1.10           : {product} (scale {product.Scale})  (multiplication ADDS scales - 2 + 1 = 3 - so nothing is silently trimmed)");

        // Non-terminating division must be told a target scale and a midpoint mode.
        var quotient = BigDecimal.Divide(BigDecimal.One, new BigDecimal(3), scale: 10, MidpointRounding.ToEven);
        Console.WriteLine($"  1 / 3 to 10 places      : {quotient}  (division cannot be exact here, so the caller must state the precision; it is never chosen silently)");

        // Round reduces the scale using the chosen midpoint rule; 2.5 rounds to even (2).
        var half = BigDecimal.Parse("2.5", CultureInfo.InvariantCulture);
        Console.WriteLine($"  Round(2.5, ToEven)      : {BigDecimal.Round(half, 0, MidpointRounding.ToEven)}  (banker\u0027s rounding - exists so a long run of .5 values does not accumulate an upward bias)");
        Console.WriteLine($"  Round(2.5, AwayFromZero): {BigDecimal.Round(half, 0, MidpointRounding.AwayFromZero)}  (the same 2.5, the other defensible answer - which is why the mode is an explicit argument)");
        Console.WriteLine($"  Round(123.45, 1 place)  : {BigDecimal.Round(price, 1, MidpointRounding.ToEven)}");

        Console.WriteLine();
    }
}
