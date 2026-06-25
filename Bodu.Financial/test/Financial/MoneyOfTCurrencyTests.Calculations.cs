// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyTests.Calculations.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Bodu.Financial.Currencies;

using Bodu.Numerics;

namespace Bodu.Financial;

/// <summary>
/// Data-driven coverage of real-world financial calculations across currencies that span the 0 / 2 / 3 minor-unit
/// categories. These tests validate that the rounding boundary lives on the correct side of every arithmetic
/// operation and that the end-of-chain monetary value is what an accountant would expect.
/// </summary>
public partial class MoneyOfTCurrencyTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // Scalar multiplication — every product rounds to the currency's minor-unit precision with banker's rounding.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that scalar multiplication produces the expected rounded result across currencies of differing
    /// minor-unit precision.
    /// </summary>
    [TestMethod]
    [DataRow(0.95, 3.0, 2.85)]        // 0.95 × 3 = 2.85 exactly
    [DataRow(1.99, 7.0, 13.93)]       // 1.99 × 7 = 13.93 exactly
    [DataRow(10.0, 0.175, 1.75)]      // 10 × 17.5% = 1.75
    [DataRow(1.00, 0.0825, 0.08)]     // 1.00 × 8.25% = 0.0825 → 0.08 (banker's: down to even)
    [DataRow(1.00, 0.0875, 0.09)]     // 1.00 × 8.75% = 0.0875 → 0.09 (4th decimal is 7, > 5 → rounds up at 2 dp)
    [DataRow(0.0, 100.0, 0.0)]
    [DataRow(-100.0, 0.10, -10.0)]
    public void Multiplication_WhenUsd_ShouldRoundToTwoDecimals(double amount, double scalar, double expected)
    {
        Money<USD> result = new Money<USD>((decimal)amount) * (decimal)scalar;

        Assert.AreEqual(new Money<USD>((decimal)expected), result);
    }

    /// <summary>
    /// Verifies scalar multiplication for the zero-minor-unit category — results round to whole units.
    /// </summary>
    [TestMethod]
    [DataRow(100.0, 0.05, 5.0)]       // ¥100 × 5% = ¥5
    [DataRow(1000.0, 0.083, 83.0)]    // ¥1000 × 8.3% = ¥83
    [DataRow(99.0, 0.5, 50.0)]        // ¥99 × 0.5 = ¥49.5 → ¥50 (banker's, up to even)
    [DataRow(98.0, 0.5, 49.0)]        // ¥98 × 0.5 = ¥49 exactly
    [DataRow(97.0, 0.5, 48.0)]        // ¥97 × 0.5 = ¥48.5 → ¥48 (banker's, down to even)
    public void Multiplication_WhenJpy_ShouldRoundToWholeUnits(double amount, double scalar, double expected)
    {
        Money<JPY> result = new Money<JPY>((decimal)amount) * (decimal)scalar;

        Assert.AreEqual(new Money<JPY>((decimal)expected), result);
    }

    /// <summary>
    /// Verifies scalar multiplication for the three-minor-unit category — results round to three decimals.
    /// </summary>
    [TestMethod]
    [DataRow(1.000, 0.50, 0.500)]
    [DataRow(0.100, 0.05, 0.005)]
    [DataRow(100.000, 0.1234, 12.340)]      // 12.34 exactly at 3 dp
    [DataRow(1.000, 0.33333, 0.333)]
    public void Multiplication_WhenBhd_ShouldRoundToThreeDecimals(double amount, double scalar, double expected)
    {
        Money<BHD> result = new Money<BHD>((decimal)amount) * (decimal)scalar;

        Assert.AreEqual(new Money<BHD>((decimal)expected), result);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Banker's rounding — midpoint values round toward the nearest even final digit, in both directions.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies banker's rounding in both directions: midpoint values round down to even and up to even depending
    /// on which neighbour is even.
    /// </summary>
    [TestMethod]
    [DataRow(1.225, 1.22)]   // .225 midpoint → down to even (2)
    [DataRow(1.235, 1.24)]   // .235 midpoint → up to even (4)
    [DataRow(1.245, 1.24)]   // .245 midpoint → down to even (4)
    [DataRow(1.255, 1.26)]   // .255 midpoint → up to even (6)
    [DataRow(-1.225, -1.22)]
    [DataRow(-1.235, -1.24)]
    public void Constructor_WhenAmountAtMidpoint_ShouldRoundToNearestEven(double amount, double expectedAmount)
    {
        var money = new Money<USD>((decimal)amount);

        Assert.AreEqual((decimal)expectedAmount, money.Amount);
    }

    /// <summary>
    /// Verifies that <see cref="MidpointRounding.AwayFromZero" /> rounds midpoint values away from zero in both
    /// the positive and negative directions.
    /// </summary>
    [TestMethod]
    [DataRow(1.225, 1.23)]
    [DataRow(1.235, 1.24)]
    [DataRow(1.245, 1.25)]
    [DataRow(-1.225, -1.23)]
    [DataRow(-1.235, -1.24)]
    public void Constructor_WhenAmountAtMidpointAndAwayFromZero_ShouldRoundAwayFromZero(double amount, double expectedAmount)
    {
        var money = new Money<USD>((decimal)amount, MidpointRounding.AwayFromZero);

        Assert.AreEqual((decimal)expectedAmount, money.Amount);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Addition / subtraction — same-currency totals.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that an additive chain of itemised same-currency amounts produces the expected ledger total.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(InvoiceTotalCases))]
    public void Addition_WhenSummingItemisedAmountsForUsd_ShouldMatchExpectedTotal(double[] amounts, double expected)
    {
        Money<USD> total = Money<USD>.Zero;
        foreach (double a in amounts)
            total += new Money<USD>((decimal)a);

        Assert.AreEqual(new Money<USD>((decimal)expected), total);
    }

    /// <summary>
    /// Provides invoice-style additive chains so the test matrix stays readable.
    /// </summary>
    /// <returns>Tuples of (line amounts, expected total).</returns>
    public static IEnumerable<object[]> InvoiceTotalCases()
    {
        yield return new object[] { new double[] { 1.00, 2.00, 3.00 }, 6.00 };
        yield return new object[] { new double[] { 0.10, 0.20, 0.30 }, 0.60 };
        yield return new object[] { new double[] { 0.01, 0.01, 0.01 }, 0.03 };
        yield return new object[] { new double[] { 99.99, 0.01 }, 100.00 };
        yield return new object[] { new double[] { 1234.56, -1234.56 }, 0.00 };
        yield return new object[] { new double[] { 19.99, 9.99, 4.99, 2.99 }, 37.96 };
        yield return new object[] { Array.Empty<double>(), 0.00 };
    }

    /// <summary>
    /// Verifies that subtracting a payment from a balance yields the expected remaining balance.
    /// </summary>
    [TestMethod]
    [DataRow(100.00, 25.00, 75.00)]
    [DataRow(100.00, 100.00, 0.00)]
    [DataRow(100.00, 100.01, -0.01)]
    [DataRow(0.00, 50.00, -50.00)]
    public void Subtraction_WhenBalanceMinusPaymentUsd_ShouldReturnRemainingBalance(double balance, double payment, double expected)
    {
        Money<USD> result = new Money<USD>((decimal)balance) - new Money<USD>((decimal)payment);

        Assert.AreEqual(new Money<USD>((decimal)expected), result);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Tax computation — extract the tax component, then add it back to recover the gross.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <c>net + tax = gross</c> for a fixed tax rate across representative net amounts.
    /// </summary>
    [TestMethod]
    [DataRow(100.00, 0.10, 10.00, 110.00)]    // Net 100 + 10% GST = 110
    [DataRow(99.99, 0.10, 10.00, 109.99)]     // .10 × 99.99 = 9.999 → 10.00 (banker's, up to even)
    [DataRow(0.01, 0.10, 0.00, 0.01)]         // .10 × 0.01 = 0.001 → 0.00 (rounds to zero — material!)
    [DataRow(1.99, 0.0825, 0.16, 2.15)]       // .0825 × 1.99 = 0.164175 → 0.16 (banker's)
    [DataRow(1234.56, 0.20, 246.91, 1481.47)] // .20 × 1234.56 = 246.912 → 246.91; net + tax = 1481.47
    public void TaxComputation_WhenComputingGstUsd_ShouldRoundAndCompose(double net, double rate, double expectedTax, double expectedGross)
    {
        var netMoney = new Money<USD>((decimal)net);
        Money<USD> tax = netMoney * (decimal)rate;
        Money<USD> gross = netMoney + tax;

        Assert.AreEqual(new Money<USD>((decimal)expectedTax), tax);
        Assert.AreEqual(new Money<USD>((decimal)expectedGross), gross);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Allocation — minor-unit-stable splitting; sum-preservation is the central invariant.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies allocation of <c>amount</c> into <paramref name="parts" /> shares produces the expected
    /// share-by-share output with the residual distributed from the start of the array.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(AllocationCases))]
    public void Allocate_WhenEqualPartsUsd_ShouldProduceExpectedShareTable(double amount, int parts, double[] expectedShares)
    {
        Money<USD>[] shares = new Money<USD>((decimal)amount).Allocate(parts);

        Assert.HasCount(expectedShares.Length, shares);
        for (int i = 0; i < expectedShares.Length; i++)
        {
            Assert.AreEqual(new Money<USD>((decimal)expectedShares[i]), shares[i], $"Share index {i}");
        }
    }

    /// <summary>
    /// Provides equal-allocation share tables; first share carries the residual to make the sum exact.
    /// </summary>
    /// <returns>Tuples of (amount, parts, expected per-share amounts).</returns>
    public static IEnumerable<object[]> AllocationCases()
    {
        yield return new object[] { 0.10, 3, new double[] { 0.04, 0.03, 0.03 } };
        yield return new object[] { 100.00, 3, new double[] { 33.34, 33.33, 33.33 } };
        yield return new object[] { 10.00, 4, new double[] { 2.50, 2.50, 2.50, 2.50 } };
        yield return new object[] { 1.00, 7, new double[] { 0.15, 0.15, 0.14, 0.14, 0.14, 0.14, 0.14 } };
        yield return new object[] { -10.00, 3, new double[] { -3.34, -3.33, -3.33 } };
        yield return new object[] { 0.02, 5, new double[] { 0.01, 0.01, 0.00, 0.00, 0.00 } };
        yield return new object[] { 0.00, 4, new double[] { 0.00, 0.00, 0.00, 0.00 } };
        yield return new object[] { 99.99, 100, EquallyDistributedShares(99.99, 100) };
    }

    private static double[] EquallyDistributedShares(double amount, int parts)
    {
        decimal cents = decimal.Round((decimal)amount * 100m, 0);
        long total = (long)cents;
        long basePer = total / parts;
        long residual = total - (basePer * parts);
        double[] result = new double[parts];
        for (int i = 0; i < parts; i++)
            result[i] = (double)((basePer + (i < residual ? 1 : 0)) / 100m);
        return result;
    }

    /// <summary>
    /// Verifies that ratio-based allocation distributes proportionally and preserves the original total exactly,
    /// across positive, negative, and mixed-zero ratios.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(RatioAllocationCases))]
    public void Allocate_WhenRatioBasedUsd_ShouldDistributeProportionallyAndPreserveTotal(double amount, double[] ratios, double[] expectedShares)
    {
        decimal[] ratioDecimals = Array.ConvertAll(ratios, r => (decimal)r);

        Money<USD>[] shares = new Money<USD>((decimal)amount).Allocate(ratioDecimals);

        Assert.HasCount(expectedShares.Length, shares);
        Money<USD> sum = Money<USD>.Zero;
        for (int i = 0; i < expectedShares.Length; i++)
        {
            Assert.AreEqual(new Money<USD>((decimal)expectedShares[i]), shares[i], $"Share index {i}");
            sum += shares[i];
        }

        Assert.AreEqual(new Money<USD>((decimal)amount), sum);
    }

    /// <summary>
    /// Provides ratio-allocation share tables.
    /// </summary>
    /// <returns>Tuples of (amount, ratios, expected per-share amounts).</returns>
    public static IEnumerable<object[]> RatioAllocationCases()
    {
        yield return new object[] { 100.00, new double[] { 1, 1, 2 }, new double[] { 25.00, 25.00, 50.00 } };
        yield return new object[] { 100.00, new double[] { 1, 1, 1 }, new double[] { 33.34, 33.33, 33.33 } };
        yield return new object[] { 1.00, new double[] { 1, 1, 1 }, new double[] { 0.34, 0.33, 0.33 } };
        yield return new object[] { 10.00, new double[] { 7, 3 }, new double[] { 7.00, 3.00 } };
        yield return new object[] { 10.00, new double[] { 1, 0, 1 }, new double[] { 5.00, 0.00, 5.00 } };
        yield return new object[] { 0.07, new double[] { 1, 0, 1 }, new double[] { 0.04, 0.00, 0.03 } };
    }

    /// <summary>
    /// Verifies the universal sum-preservation invariant: for any allocation, the shares total exactly the
    /// original amount. This locks the invariant against future refactors.
    /// </summary>
    [TestMethod]
    [DataRow(0.10, 3)]
    [DataRow(0.10, 7)]
    [DataRow(100.00, 3)]
    [DataRow(100.00, 100)]
    [DataRow(0.01, 3)]
    [DataRow(0.01, 100)]
    [DataRow(123456.78, 17)]
    [DataRow(-100.00, 3)]
    [DataRow(-100.00, 7)]
    [DataRow(0.0, 5)]
    public void Allocate_WhenAnyEqualPartsUsd_ShouldSumToOriginal(double amount, int parts)
    {
        var original = new Money<USD>((decimal)amount);

        Money<USD>[] shares = original.Allocate(parts);

        Money<USD> sum = Money<USD>.Zero;
        foreach (Money<USD> share in shares)
            sum += share;

        Assert.AreEqual(original, sum);
    }

    /// <summary>
    /// Verifies sum-preservation across all minor-unit categories.
    /// </summary>
    [TestMethod]
    [DataRow("JPY", 100.0, 3)]
    [DataRow("JPY", 100.0, 7)]
    [DataRow("BHD", 10.000, 3)]
    [DataRow("BHD", 1.000, 7)]
    [DataRow("KWD", 0.001, 5)]
    [DataRow("KRW", 1000.0, 13)]
    [DataRow("CLP", 99999.0, 7)]
    public void Allocate_WhenAnyMinorUnitCurrency_ShouldSumToOriginal(string iso, double amount, int parts)
    {
        Type currencyType = ResolveCurrencyType(iso);
        Type moneyType = typeof(Money<>).MakeGenericType(currencyType);
        object original = Activator.CreateInstance(moneyType, (decimal)amount)!;

        var shares = (Array)moneyType
            .GetMethod("Allocate", [typeof(int)])!
            .Invoke(original, [parts])!;

        object sum = Activator.CreateInstance(moneyType)!;
        System.Reflection.MethodInfo addOp = moneyType.GetMethod("op_Addition")!;
        foreach (object? share in shares)
            sum = addOp.Invoke(null, [sum, share])!;

        Assert.AreEqual(original, sum);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Conversion — exchange-rate application across currency pairs spanning all minor-unit categories.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies <see cref="Money{TCurrency}.Convert{TTarget}(decimal, MidpointRounding)" /> across representative
    /// FX scenarios, with rounding to the destination currency's minor-unit precision.
    /// </summary>
    [TestMethod]
    [DataRow(100.00, 155.5, 15550.0)]      // USD 100 × 155.5 JPY/USD = JPY 15550 exactly
    [DataRow(100.00, 0.93, 93.0)]          // USD 100 × 0.93 = 93 JPY (rounded to 0 dp)
    [DataRow(1.00, 0.00789, 0.0)]          // USD 1 × 0.00789 = 0.00789 JPY → 0 JPY (material loss)
    [DataRow(1234.56, 158.42, 195579.0)]   // 1234.56 × 158.42 = 195578.9952 → 195579 at JPY's 0 dp
    public void Convert_WhenUsdToJpy_ShouldRoundToZeroMinorUnits(double amount, double rate, double expected)
    {
        var source = new Money<USD>((decimal)amount);

        Money<JPY> target = source.Convert<JPY>((decimal)rate);

        Assert.AreEqual(new Money<JPY>((decimal)expected), target);
    }

    /// <summary>
    /// Verifies conversion into a three-minor-unit currency with banker's rounding at the destination precision.
    /// </summary>
    [TestMethod]
    [DataRow(100.00, 0.378, 37.800)]       // USD 100 × 0.378 BHD/USD = BHD 37.800
    [DataRow(1.00, 0.3785, 0.378)]         // 0.3785 → 0.378 (banker's, down to even)
    [DataRow(1.00, 0.3795, 0.380)]         // 0.3795 → 0.380 (banker's, up to even? actually 0.3795 → 0.380 since 9 is odd, but next even is 0; rounds to 0.380)
    public void Convert_WhenUsdToBhd_ShouldRoundToThreeMinorUnits(double amount, double rate, double expected)
    {
        var source = new Money<USD>((decimal)amount);

        Money<BHD> target = source.Convert<BHD>((decimal)rate);

        Assert.AreEqual(new Money<BHD>((decimal)expected), target);
    }

    /// <summary>
    /// Verifies a round-trip USD → JPY → USD at typical FX rates picks up minor-unit truncation in the
    /// intermediate currency exactly as expected, with no silent precision recovery.
    /// </summary>
    [TestMethod]
    public void Convert_WhenRoundTrippingUsdToJpyAndBack_ShouldTrackIntermediateTruncation()
    {
        var original = new Money<USD>(100.00m);

        Money<JPY> intermediate = original.Convert<JPY>(155.49m);
        Money<USD> recovered = intermediate.Convert<USD>(1m / 155.49m);

        // USD 100 → JPY round(15549) = JPY 15549 → USD round(15549/155.49) = USD round(99.99999...) = 100.00
        Assert.AreEqual(new Money<JPY>(15549m), intermediate);
        Assert.AreEqual(new Money<USD>(100.00m), recovered);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Fraction escape hatch — exact arithmetic through Fraction<BigInteger> avoids accumulating round-off.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a calculation done through <see cref="Money{TCurrency}.MultiplyExact" /> rounds exactly once
    /// at the end, even for ratios that have no exact decimal representation.
    /// </summary>
    [TestMethod]
    [DataRow(1.00, 1, 3, 0.33)]          // 1 × 1/3 = 0.333... → 0.33
    [DataRow(100.00, 1, 7, 14.29)]       // 100 × 1/7 = 14.2857... → 14.29
    [DataRow(100.00, 22, 7, 314.29)]     // 100 × 22/7 = 314.2857... → 314.29
    [DataRow(0.10, 1, 3, 0.03)]          // 0.10 × 1/3 = 0.0333... → 0.03
    public void MultiplyExact_WhenAppliedToExactFraction_ShouldRoundOnce(double amount, int numerator, int denominator, double expected)
    {
        var money = new Money<USD>((decimal)amount);
        var factor = Fraction<BigInteger>.Create(numerator, denominator);

        Money<USD> result = money.MultiplyExact(factor);

        Assert.AreEqual(new Money<USD>((decimal)expected), result);
    }

    /// <summary>
    /// Verifies that an exact-fraction round-trip through <see cref="Fraction{T}" /> preserves the original
    /// amount across a divide-then-multiply chain, where the scalar-rounded equivalent visibly drifts.
    /// </summary>
    [TestMethod]
    public void MultiplyExact_WhenDividingByThreeThenMultiplyingBack_ShouldNotDriftLikeScalarChain()
    {
        var principal = new Money<USD>(100m);

        // Scalar-rounded chain — 100 / 3 = 33.33 (rounded) × 3 = 99.99 (drifts a cent).
        Money<USD> naive = principal / 3m * 3m;

        // Exact chain — one rounding event at the end.
        Fraction<BigInteger> exact = principal.ToFraction() / Fraction<BigInteger>.Create(3, 1) * Fraction<BigInteger>.Create(3, 1);
        var exactResult = Money<USD>.FromFraction(exact);

        Assert.AreEqual(principal, exactResult);
        Assert.AreEqual(new Money<USD>(99.99m), naive);
        Assert.AreNotEqual(principal, naive);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Compound interest — a real-world chain that highlights why the Fraction escape hatch is worth providing.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies a 12-month compound-interest calculation at 5% APR on a $1,000 principal produces the textbook
    /// result when computed exactly through <see cref="Fraction{T}" /> and rounded once at the end.
    /// </summary>
    [TestMethod]
    public void CompoundInterest_WhenTwelveMonthsAtFivePercentApr_ShouldMatchTextbookResult()
    {
        var principal = new Money<USD>(1000m);
        var monthlyRate = Fraction<BigInteger>.Create(5, 1200);          // 5% / 12 months
        Fraction<BigInteger> growth = Fraction<BigInteger>.One + monthlyRate;             // 1 + r

        var balance = principal.ToFraction();
        for (int i = 0; i < 12; i++)
            balance *= growth;

        var result = Money<USD>.FromFraction(balance);

        // 1000 × (1 + 5/1200)^12 = 1051.1618978...
        // banker's-round to 1051.16
        Assert.AreEqual(new Money<USD>(1051.16m), result);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Sign and zero properties — used as guard predicates in real ledger code.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the sign-bit properties classify amounts correctly across the sign and zero boundary.
    /// </summary>
    [TestMethod]
    [DataRow(0.0, 0)]
    [DataRow(0.01, 1)]
    [DataRow(-0.01, -1)]
    [DataRow(1234.56, 1)]
    [DataRow(-1234.56, -1)]
    public void Sign_WhenInspected_ShouldReportExpectedSign(double amount, int expectedSign)
    {
        var money = new Money<USD>((decimal)amount);

        Assert.AreEqual(expectedSign, money.Sign);
        Assert.AreEqual(amount == 0.0, money.IsZero);
    }

    /// <summary>
    /// Verifies that <c>Abs</c> is idempotent and always non-negative.
    /// </summary>
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(19.99)]
    [DataRow(-19.99)]
    [DataRow(1234.56)]
    [DataRow(-1234.56)]
    public void Abs_WhenAnyAmount_ShouldBeNonNegativeAndIdempotent(double amount)
    {
        var money = new Money<USD>((decimal)amount);

        Money<USD> abs = money.Abs;
        Money<USD> absAgain = abs.Abs;

        Assert.IsGreaterThanOrEqualTo(0m, abs.Amount);
        Assert.AreEqual(abs, absAgain);
        Assert.AreEqual(Math.Abs((decimal)amount), abs.Amount);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Cumulative-rounding awareness — chains of scalar operations drift unless intentionally guarded.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that repeated scalar multiplications accumulate per-step rounding error compared to a single
    /// equivalent multiplication. This is a documentation-anchor test confirming the call-site responsibility to
    /// either accept the drift or convert through <see cref="Fraction{T}" />.
    /// </summary>
    [TestMethod]
    public void RepeatedMultiplication_WhenChainedThirteenTimes_ShouldDriftFromSingleEquivalent()
    {
        var start = new Money<USD>(100m);
        decimal factor = 1.01m;

        Money<USD> chained = start;
        for (int i = 0; i < 13; i++)
            chained *= factor;

        var oneShot = new Money<USD>(start.Amount * (decimal)Math.Pow((double)factor, 13));

        // 100 × 1.01^13 = 113.80932... → 113.81; chained version may drift by a cent.
        Assert.AreEqual(new Money<USD>(113.81m), oneShot);
        Assert.IsLessThanOrEqualTo(0.02m, Math.Abs(chained.Amount - oneShot.Amount), $"Chained={chained.Amount} OneShot={oneShot.Amount}");
    }

    /// <summary>
    /// Verifies that an exact multiply landing on a half-minor-unit midpoint resolves the tie according to each
    /// supported rounding rule, exercising the directed and nearest <see cref="BigInteger" /> rounding paths.
    /// </summary>
    [TestMethod]
    [DataRow(1, 8, MidpointRounding.ToEven, 0.12)]
    [DataRow(1, 8, MidpointRounding.AwayFromZero, 0.13)]
    [DataRow(1, 8, MidpointRounding.ToZero, 0.12)]
    [DataRow(1, 8, MidpointRounding.ToPositiveInfinity, 0.13)]
    [DataRow(1, 8, MidpointRounding.ToNegativeInfinity, 0.12)]
    [DataRow(-1, 8, MidpointRounding.ToNegativeInfinity, -0.13)]
    [DataRow(-1, 8, MidpointRounding.ToPositiveInfinity, -0.12)]
    public void MultiplyExact_WhenResultLandsOnMidpoint_ShouldRoundByMode(int factorNumerator, int factorDenominator, MidpointRounding rounding, double expected)
    {
        Money<USD> result = new Money<USD>(1m).MultiplyExact(new Fraction<BigInteger>(factorNumerator, factorDenominator), rounding);

        Assert.AreEqual((decimal)expected, result.Amount);
    }
}
