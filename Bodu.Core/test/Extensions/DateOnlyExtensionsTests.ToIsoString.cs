// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.ToIsoString.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------


using System;
using System.Globalization;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.ToIsoString" />, when UtcKind, returns the expected value.
    /// </summary>
    [TestMethod]
    [DataRow("2024-04-20T15:30:45.1234560Z", DateTimeKind.Utc, "2024-04-20T15:30:45.1234560Z")]
    public void ToIsoString_WhenUtcKind_ShouldReturnUtcFormatted(string inputStr, DateTimeKind kind, string expected)
    {
        DateTime input = DateTime.Parse(inputStr, null, DateTimeStyles.AdjustToUniversal).ToUniversalTime();
        var actual = input.ToIsoString();
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.ToIsoString" />, with CustomFormat, returns the expected value.
    /// </summary>
    [TestMethod]
    [DataRow("2024-04-20T15:30:45", "yyyy-MM-dd", "2024-04-20")]
    [DataRow("2024-04-20T15:30:45", "HH:mm:ss", "15:30:45")]
    public void ToIsoString_WithCustomFormat_ShouldReturnExpected(string inputStr, string format, string expected)
    {
        var input = DateTime.Parse(inputStr, CultureInfo.InvariantCulture);
        var actual = input.ToIsoString(format);
        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.ToIsoString" />, with EmptyFormat, throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ToIsoString_WithEmptyFormat_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 4, 20);
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            input.ToIsoString(string.Empty);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.ToIsoString" />, with ExplicitKind, returns the expected value.
    /// </summary>
    [TestMethod]
    [DataRow("2024-04-20T15:30:45", DateTimeKind.Utc, "2024-04-20T15:30:45.0000000Z")]
    [DataRow("2024-04-20T15:30:45", DateTimeKind.Local, null)]
    [DataRow("2024-04-20T15:30:45", DateTimeKind.Unspecified, "2024-04-20T15:30:45.0000000")]
    public void ToIsoString_WithExplicitKind_ShouldRespectKind(string dateTimeStr, DateTimeKind kind, string? expected)
    {
        var input = DateTime.SpecifyKind(DateTime.Parse(dateTimeStr, CultureInfo.InvariantCulture), kind);
        var actual = input.ToIsoString(kind);

        if (expected != null)
        {
            Assert.AreEqual(expected, actual);
        }
        else
        {
            // Parse back and confirm the kind is preserved
            var parsed = DateTime.Parse(actual, null, DateTimeStyles.RoundtripKind);
            Assert.AreEqual(DateTimeKind.Local, parsed.Kind, "Expected kind mismatch for local input.");
            Assert.StartsWith("2024-04-20T15:30:45", actual, "Expected prefix missing.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.ToIsoString" />, with IncludeFractionalSeconds, returns the expected value.
    /// </summary>
    [TestMethod]
    [DataRow("2024-04-20T15:30:45", false, "2024-04-20T15:30:45")]
    [DataRow("2024-04-20T15:30:45.1234567", true, null)] // Validate that it returns a valid round-trip
    public void ToIsoString_WithIncludeFractionalSeconds_ShouldRespectOption(string dateTimeStr, bool includeFraction, string? expected)
    {
        var input = DateTime.Parse(dateTimeStr, CultureInfo.InvariantCulture);
        var actual = input.ToIsoString(includeFraction);

        if (expected != null)
            Assert.AreEqual(expected, actual);
        else
            Assert.StartsWith("2024-04-20T15:30:45.", actual);
    }

}
