// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyTests.TestImprovements.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using Bodu.Financial.Currencies;

using Bodu.Numerics;

namespace Bodu.Financial;

/// <summary>
/// Coverage gaps surfaced by the test-review document: JSON malformed-payload grid, TryFormat exact-buffer
/// boundaries, allocation overflow / range edges, and a property-based allocation sweep.
/// </summary>
public partial class MoneyOfTCurrencyTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // JSON malformed-payload grid — what the deserialiser does with every reasonable wrong shape.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that every malformed JSON payload that the test-review document called out is rejected with
    /// <see cref="JsonException" /> rather than silently producing a default <see cref="Money{TCurrency}" />.
    /// </summary>
    [TestMethod]
    [DataRow("null")]                                                  // top-level null
    [DataRow("[]")]                                                    // wrong root shape — array
    [DataRow("{}")]                                                    // empty object
    [DataRow("{\"amount\":19.99}")]                                    // missing currency
    [DataRow("{\"currency\":\"USD\"}")]                                // missing amount
    [DataRow("{\"amount\":null,\"currency\":\"USD\"}")]                // amount is null
    [DataRow("{\"amount\":true,\"currency\":\"USD\"}")]                // amount is boolean
    [DataRow("{\"amount\":{},\"currency\":\"USD\"}")]                  // amount is object
    [DataRow("{\"amount\":[],\"currency\":\"USD\"}")]                  // amount is array
    [DataRow("{\"amount\":\"not-a-number\",\"currency\":\"USD\"}")]    // amount is a non-numeric string
    [DataRow("{\"amount\":19.99,\"currency\":null}")]                  // currency is null
    [DataRow("{\"amount\":19.99,\"currency\":123}")]                   // currency is a number
    [DataRow("{\"amount\":19.99,\"currency\":\"usd\"}")]               // currency case mismatch
    [DataRow("{\"amount\":19.99,\"currency\":\" USD \"}")]             // currency with surrounding whitespace
    public void JsonDeserialize_WhenMalformedPayload_ShouldThrowJsonException(string json)
    {
        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money<USD>>(json);
        }, $"Payload should not deserialise: {json}");
    }

    /// <summary>
    /// Verifies that the reader continues to accept a numeric <c>"amount"</c> emitted as a JSON string —
    /// documented lenient behaviour for systems lacking arbitrary-precision number support.
    /// </summary>
    [TestMethod]
    public void JsonDeserialize_WhenAmountIsNumericString_ShouldAcceptIt()
    {
        Money<USD> money = JsonSerializer.Deserialize<Money<USD>>("{\"amount\":\"19.99\",\"currency\":\"USD\"}");

        Assert.AreEqual(new Money<USD>(19.99m), money);
    }

    /// <summary>
    /// Verifies that an over-precision input is interpreted at the currency's precision, matching the
    /// documented settlement-value contract. (Strict over-precision rejection was considered and declined for
    /// v1 — the type is documented as a settlement value that interprets any input at its precision.)
    /// </summary>
    [TestMethod]
    public void JsonDeserialize_WhenAmountHasOverPrecision_ShouldNormaliseToMinorUnits()
    {
        Money<USD> money = JsonSerializer.Deserialize<Money<USD>>("{\"amount\":1.999,\"currency\":\"USD\"}");

        Assert.AreEqual(new Money<USD>(2.00m), money);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // TryFormat exact-buffer boundaries.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ISpanFormattable.TryFormat" /> succeeds when the destination span is exactly
    /// the required length.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenDestinationLengthExactlyMatchesRequired_ShouldSucceed()
    {
        var money = new Money<USD>(19.99m);
        string expected = money.ToString(null, CultureInfo.InvariantCulture);
        Span<char> buffer = stackalloc char[expected.Length];

        bool ok = money.TryFormat(buffer, out int written, default, CultureInfo.InvariantCulture);

        Assert.IsTrue(ok);
        Assert.AreEqual(expected.Length, written);
        Assert.AreEqual(expected, buffer[..written].ToString());
    }

    /// <summary>
    /// Verifies that <see cref="ISpanFormattable.TryFormat" /> reports failure when the destination is one
    /// character short.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenDestinationOneCharShort_ShouldReturnFalse()
    {
        var money = new Money<USD>(19.99m);
        string expected = money.ToString(null, CultureInfo.InvariantCulture);
        Span<char> buffer = stackalloc char[expected.Length - 1];

        bool ok = money.TryFormat(buffer, out int written, default, CultureInfo.InvariantCulture);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="ISpanFormattable.TryFormat" /> reports failure with a zero-length destination.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenDestinationLengthZero_ShouldReturnFalse()
    {
        var money = new Money<USD>(19.99m);
        Span<char> buffer = [];

        bool ok = money.TryFormat(buffer, out int written, default, CultureInfo.InvariantCulture);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies the corresponding boundary on <see cref="IUtf8SpanFormattable.TryFormat" /> using the UTF-8
    /// byte length.
    /// </summary>
    [TestMethod]
    public void TryFormatUtf8_WhenDestinationExactlyMatchesByteLength_ShouldSucceed()
    {
        var money = new Money<USD>(19.99m);
        string expected = money.ToString(null, CultureInfo.InvariantCulture);
        int byteLength = System.Text.Encoding.UTF8.GetByteCount(expected);
        Span<byte> buffer = stackalloc byte[byteLength];

        bool ok = money.TryFormat(buffer, out int written, default, CultureInfo.InvariantCulture);

        Assert.IsTrue(ok);
        Assert.AreEqual(byteLength, written);
    }

    /// <summary>
    /// Verifies the UTF-8 fail-short boundary.
    /// </summary>
    [TestMethod]
    public void TryFormatUtf8_WhenDestinationOneByteShort_ShouldReturnFalse()
    {
        var money = new Money<USD>(19.99m);
        string expected = money.ToString(null, CultureInfo.InvariantCulture);
        int byteLength = System.Text.Encoding.UTF8.GetByteCount(expected);
        Span<byte> buffer = stackalloc byte[byteLength - 1];

        bool ok = money.TryFormat(buffer, out int written, default, CultureInfo.InvariantCulture);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Allocation overflow + range edges.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that allocating an amount whose scaled minor-unit count would exceed <see cref="long" /> allocates
    /// exactly via <see cref="System.Numerics.BigInteger" /> scaling, with the shares summing to the original.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenScaledMinorUnitsExceedLong_ShouldAllocateExactlyAndSumToOriginal()
    {
        var huge = new Money<USD>(decimal.MaxValue / 2m);

        Money<USD>[] shares = huge.Allocate(2);

        Assert.AreEqual(2, shares.Length);
        Assert.AreEqual(shares[0], shares[1]);
        Assert.AreEqual(huge, shares[0] + shares[1]);
    }

    /// <summary>
    /// Verifies that allocations near the upper end of the supported range (long.MaxValue / 100 minor units)
    /// succeed without truncation.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenAmountIsNearLongLimit_ShouldSucceedAndPreserveSum()
    {
        // long.MaxValue ≈ 9.22 × 10^18. For USD (2 dp), that supports up to ~9.22 × 10^16 dollars.
        var nearLimit = new Money<USD>(90_000_000_000_000_000m);

        Money<USD>[] shares = nearLimit.Allocate(7);

        Money<USD> sum = Money<USD>.Zero;
        foreach (Money<USD> share in shares)
            sum += share;

        Assert.AreEqual(nearLimit, sum);
    }

    /// <summary>
    /// Verifies the sum-preservation invariant across a wide random sweep of (amount, parts) inputs. Locks the
    /// invariant against any future regression in the allocator's residual distribution.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenRandomisedSweep_ShouldAlwaysPreserveSum()
    {
        Random rng = new(20260530);
        for (int i = 0; i < 500; i++)
        {
            int sign = rng.Next(0, 3) - 1;    // -1, 0, 1
            decimal cents = rng.Next(0, 1_000_001) * 0.01m * sign;
            int parts = rng.Next(1, 50);
            var original = new Money<USD>(cents);

            Money<USD>[] shares = original.Allocate(parts);

            Money<USD> sum = Money<USD>.Zero;
            foreach (Money<USD> share in shares)
                sum += share;

            Assert.AreEqual(original, sum, $"Iteration {i}: amount={cents}, parts={parts}");
        }
    }

    /// <summary>
    /// Verifies the same sum-preservation property for ratio-based allocation across a random sweep.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenRatioBasedRandomisedSweep_ShouldAlwaysPreserveSum()
    {
        Random rng = new(20260530);
        for (int i = 0; i < 200; i++)
        {
            decimal cents = rng.Next(-1_000_000, 1_000_001) * 0.01m;
            int slotCount = rng.Next(2, 11);
            decimal[] ratios = new decimal[slotCount];
            for (int j = 0; j < slotCount; j++)
                ratios[j] = rng.Next(0, 10);

            if (ratios.All(r => r == 0m))
                ratios[0] = 1m;     // guarantee at least one positive ratio

            var original = new Money<USD>(cents);

            Money<USD>[] shares = original.Allocate(ratios);

            Money<USD> sum = Money<USD>.Zero;
            foreach (Money<USD> share in shares)
                sum += share;

            Assert.AreEqual(original, sum, $"Iteration {i}: amount={cents}, ratios={string.Join(",", ratios)}");
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Invalid metadata flows through every metadata-dependent surface — not just the constructor.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the centralised metadata validator catches invalid currency metadata at every
    /// metadata-dependent surface — Money static properties, Money instance methods, and JSON serialisation —
    /// not only at the constructor.
    /// </summary>
    [TestMethod]
    public void InvalidMetadata_WhenAccessedFromAnyMetadataDependentSurface_ShouldThrow()
    {
        // Static properties:
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Money<NullIsoCurrency>.IsoCode; });
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Money<NullIsoCurrency>.MinorUnits; });
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Money<NullIsoCurrency>.CashRoundingIncrement; });
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Money<NullIsoCurrency>.IsHistoric; });

        // Constructors:
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = new Money<NullIsoCurrency>(1m); });
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = new Money<NullIsoCurrency>(1m, MidpointRounding.AwayFromZero); });

        // FromMinorUnits / ToMinorUnits:
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Money<NullIsoCurrency>.FromMinorUnits(100); });

        // FromFraction:
        Assert.ThrowsExactly<InvalidOperationException>(() => { _ = Money<NullIsoCurrency>.FromFraction(Fraction<System.Numerics.BigInteger>.One); });

        // JSON write of a default value (whose payload depends on metadata):
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = JsonSerializer.Serialize(default(Money<NullIsoCurrency>));
        });
    }
}
