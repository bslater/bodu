// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.TryGetSingle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies <see cref="Utf8TomlReader.TryGetSingle" />, including a float literal that does not narrow to
/// <see cref="float" />.
/// </summary>
public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.TryGetSingle" /> returns <see langword="false" /> for a finite float
    /// literal outside the <see cref="float" /> range, just past its largest finite magnitude or far past it, positive
    /// or negative, rather than reading it as an infinity.
    /// </summary>
    /// <param name="literal">The float literal outside the range.</param>
    [TestMethod]
    [DataRow("3.4028236e38", DisplayName = "just past float.MaxValue")]
    [DataRow("-3.4028236e38", DisplayName = "just past float.MinValue")]
    [DataRow("1e300", DisplayName = "far past float.MaxValue")]
    [DataRow("-1e300", DisplayName = "far past float.MinValue")]
    public void TryGetSingle_WhenLiteralIsOutsideSingleRange_ShouldReturnFalse(string literal)
    {
        Utf8TomlReader reader = Create($"f = {literal}\n");
        Advance(ref reader, 2);

        Assert.IsFalse(reader.TryGetSingle(out _));
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.TryGetSingle" /> reads a float literal that rounds to the largest finite
    /// <see cref="float" /> magnitude as that value.
    /// </summary>
    /// <param name="literal">The float literal at the edge of the range.</param>
    /// <param name="expected">The expected value.</param>
    [TestMethod]
    [DataRow("3.4028235e38", float.MaxValue, DisplayName = "rounds to float.MaxValue")]
    [DataRow("-3.4028235e38", float.MinValue, DisplayName = "rounds to float.MinValue")]
    public void TryGetSingle_WhenLiteralRoundsToLargestFiniteValue_ShouldReturnThatValue(string literal, float expected)
    {
        Utf8TomlReader reader = Create($"f = {literal}\n");
        Advance(ref reader, 2);

        bool converted = reader.TryGetSingle(out float value);

        Assert.AreEqual((true, expected), (converted, value));
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.TryGetSingle" /> reads <c>inf</c>, <c>-inf</c> and <c>nan</c> as the
    /// matching <see cref="float" /> values.
    /// </summary>
    /// <param name="literal">The special float literal.</param>
    /// <param name="expected">The expected value.</param>
    [TestMethod]
    [DataRow("inf", float.PositiveInfinity, DisplayName = "inf")]
    [DataRow("-inf", float.NegativeInfinity, DisplayName = "-inf")]
    [DataRow("nan", float.NaN, DisplayName = "nan")]
    public void TryGetSingle_WhenLiteralIsInfinityOrNaN_ShouldReturnMatchingValue(string literal, float expected)
    {
        Utf8TomlReader reader = Create($"f = {literal}\n");
        Advance(ref reader, 2);

        bool converted = reader.TryGetSingle(out float value);

        Assert.AreEqual((true, expected), (converted, value));
    }
}
