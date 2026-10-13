// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceSetTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public partial class RecurrenceSetTests
{
    /// <summary>
    /// Verifies that <see cref="RecurrenceSet.Parse(string)" /> throws <see cref="FormatException" /> whose message is
    /// the defect message the defect-reporting <c>TryParse</c> overload reports for the same block.
    /// </summary>
    /// <param name="kat">The malformed property-block row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(TryParseDefectKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Parse_WhenInvalid_ShouldThrowFormatExceptionNamingTheDefect(InvalidKat<string> kat)
    {
        _ = RecurrenceSet.TryParse(kat.Input, out _, out string? failureMessage);

        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = RecurrenceSet.Parse(kat.Input);
        });

        Assert.AreEqual(failureMessage, ex.Message);
        Assert.IsTrue(
            ex.Message.Contains(kat.MessageContains!, StringComparison.Ordinal),
            $"Expected message to contain '{kat.MessageContains}' but was '{ex.Message}'.");
    }

    /// <summary>
    /// Verifies that <see cref="RecurrenceSet.Parse(string)" /> throws <see cref="ArgumentNullException" /> for a
    /// <see langword="null" /> block.
    /// </summary>
    [TestMethod]
    public void Parse_WhenNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = RecurrenceSet.Parse(null!);
        });
    }
}
