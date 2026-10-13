// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvariantScalarCodecTests.Format.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

using Bodu.Test.Kat;

namespace Bodu.Text.Serialization;

public sealed partial class InvariantScalarCodecTests
{
    /// <summary>
    /// Verifies that a decimal is written with the invariant separator even when the current culture uses a comma.
    /// </summary>
    [TestMethod]
    public void Format_WhenCurrentCultureUsesCommaSeparator_ForConfigurationPolicy_ShouldWriteInvariantText()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");

            Assert.AreEqual("1.25", InvariantScalarCodec.Format(1.25m, InvariantScalarCodec.Policy.Configuration));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// Verifies that a <see cref="bool" /> is written in lower case.
    /// </summary>
    [TestMethod]
    public void Format_WhenValueIsBoolean_ForConfigurationPolicy_ShouldWriteLowerCase()
    {
        Assert.AreEqual("false", InvariantScalarCodec.Format(false, InvariantScalarCodec.Policy.Configuration));
    }

    /// <summary>
    /// Verifies that a <see cref="DateTime" /> is written in the round-trip form, which keeps its sub-second ticks.
    /// </summary>
    [TestMethod]
    public void Format_WhenDateTimeHasTicks_ForDelimitedPolicy_ShouldWriteRoundTripText()
    {
        string text = InvariantScalarCodec.Format(s_instant, InvariantScalarCodec.Policy.Delimited);

        Assert.AreEqual(s_instant.ToString("O", CultureInfo.InvariantCulture), text);
    }

    /// <summary>
    /// Gets a value of every scalar type, and <see langword="null" />, each with the text it is written as under the
    /// configuration policy and under the delimited policy.
    /// </summary>
    /// <value>The rows, each a named value and policy, and the text written.</value>
    public static IEnumerable<object[]> ScalarForms =>
    [
        .. Forms("null", null, string.Empty, string.Empty),
        .. Forms("string", "text", "text", "text"),
        .. Forms("bool", true, "true", "true"),
        .. Forms("char", 'c', "c", "c"),
        .. Forms("int", -42, "-42", "-42"),
        .. Forms("double", 1.5, "1.5", "1.5"),
        .. Forms("decimal", 1.25m, "1.25", "1.25"),
        .. Forms("nint", (nint)(-7), "-7", "-7"),
        .. Forms("nuint", (nuint)7, "7", "7"),
        .. Forms("Int128", Int128.MinValue, "-170141183460469231731687303715884105728", "-170141183460469231731687303715884105728"),
        .. Forms("UInt128", UInt128.MaxValue, "340282366920938463463374607431768211455", "340282366920938463463374607431768211455"),
        .. Forms("Half", (Half)0.5, "0.5", "0.5"),
        .. Forms("BigInteger", System.Numerics.BigInteger.Parse("12345678901234567890123", CultureInfo.InvariantCulture), "12345678901234567890123", "12345678901234567890123"),
        .. Forms("Guid", new Guid("01234567-89ab-cdef-0123-456789abcdef"), "01234567-89ab-cdef-0123-456789abcdef", "01234567-89ab-cdef-0123-456789abcdef"),
        .. Forms("DateTime", s_instant, "2026-08-20T11:22:33.1234567Z", "2026-08-20T11:22:33.1234567Z"),
        .. Forms("DateTimeOffset", new DateTimeOffset(s_instant.Ticks, TimeSpan.FromHours(5.5)), "2026-08-20T11:22:33.1234567+05:30", "2026-08-20T11:22:33.1234567+05:30"),
        .. Forms("DateOnly", new DateOnly(2026, 1, 2), "01/02/2026", "2026-01-02"),
        .. Forms("TimeOnly", new TimeOnly(13, 45, 30).Add(TimeSpan.FromTicks(1234567)), "13:45:30.1234567", "13:45:30.1234567"),
        .. Forms("TimeSpan", new TimeSpan(1, 2, 3, 4, 500), "1.02:03:04.5000000", "1.02:03:04.5000000"),
        .. Forms("Uri", new Uri("https://example.com/a"), "https://example.com/a", "https://example.com/a"),
        .. Forms("enum", DayOfWeek.Tuesday, "Tuesday", "Tuesday"),
    ];

    /// <summary>
    /// Verifies that a value of every scalar type, and <see langword="null" />, is written in its invariant form under
    /// each policy.
    /// </summary>
    /// <param name="kat">The row holding the value, the policy and the text written.</param>
    [TestMethod]
    [DynamicData(
        nameof(ScalarForms),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Format_WhenTheValueIsAScalar_ShouldWriteItsInvariantForm(ValidKat<(object? Value, bool Delimited), string> kat)
    {
        InvariantScalarCodec.Policy policy = kat.Input.Delimited ? InvariantScalarCodec.Policy.Delimited : InvariantScalarCodec.Policy.Configuration;

        Assert.AreEqual(kat.Expected, InvariantScalarCodec.Format(kat.Input.Value, policy));
    }

    /// <summary>
    /// Creates the rows for one value: its text under the configuration policy and under the delimited policy.
    /// </summary>
    /// <param name="name">The name of the value's type.</param>
    /// <param name="value">The value.</param>
    /// <param name="configuration">The text written under the configuration policy.</param>
    /// <param name="delimited">The text written under the delimited policy.</param>
    /// <returns>The two rows.</returns>
    private static IEnumerable<object[]> Forms(string name, object? value, string configuration, string delimited) =>
    [
        [new ValidKat<(object? Value, bool Delimited), string>($"{name}, configuration", (value, false), configuration)],
        [new ValidKat<(object? Value, bool Delimited), string>($"{name}, delimited", (value, true), delimited)],
    ];
}
