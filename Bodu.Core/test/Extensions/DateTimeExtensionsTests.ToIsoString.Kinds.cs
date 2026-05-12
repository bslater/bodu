// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.ToIsoString.Kinds.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.ToIsoString(DateTime)" /> uses the round-trip <c>"o"</c> format for a
    /// <see cref="DateTimeKind.Local" /> value, producing a string that round-trips back to <see cref="DateTimeKind.Local" />.
    /// </summary>
    [TestMethod]
    public void ToIsoString_NoArgs_WhenKindIsLocal_ShouldUseRoundTripFormat()
    {
        var input = DateTime.SpecifyKind(new DateTime(2024, 4, 20, 15, 30, 45), DateTimeKind.Local);
        string actual = input.ToIsoString();

        // Round-trip and confirm Kind is preserved as Local.
        var parsed = DateTime.Parse(actual, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind);
        Assert.AreEqual(DateTimeKind.Local, parsed.Kind);
        Assert.IsTrue(actual.StartsWith("2024-04-20T15:30:45"));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.ToIsoString(DateTime)" /> uses the round-trip <c>"o"</c> format for a
    /// <see cref="DateTimeKind.Unspecified" /> value, omitting any offset suffix.
    /// </summary>
    [TestMethod]
    public void ToIsoString_NoArgs_WhenKindIsUnspecified_ShouldOmitOffset()
    {
        var input = DateTime.SpecifyKind(new DateTime(2024, 4, 20, 15, 30, 45), DateTimeKind.Unspecified);
        string actual = input.ToIsoString();

        Assert.AreEqual("2024-04-20T15:30:45.0000000", actual);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.ToIsoString(DateTime, DateTimeKind)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the supplied <see cref="DateTimeKind" /> is not a defined enum value.
    /// </summary>
    [TestMethod]
    public void ToIsoString_WithExplicitKind_WhenKindIsUndefined_ShouldThrowExactly()
    {
        var input = new DateTime(2024, 4, 20);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = input.ToIsoString((DateTimeKind)99);
        });
    }
}
