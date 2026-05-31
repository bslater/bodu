// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfInvalidStringComparison.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfInvalidStringComparison" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for each of the six canonical
    /// <see cref="StringComparison" /> values.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="comparison">The <see cref="StringComparison" /> passed to the guard.</param>
    [TestMethod]
    [DataRow("CurrentCulture", StringComparison.CurrentCulture)]
    [DataRow("CurrentCultureIgnoreCase", StringComparison.CurrentCultureIgnoreCase)]
    [DataRow("InvariantCulture", StringComparison.InvariantCulture)]
    [DataRow("InvariantCultureIgnoreCase", StringComparison.InvariantCultureIgnoreCase)]
    [DataRow("Ordinal", StringComparison.Ordinal)]
    [DataRow("OrdinalIgnoreCase", StringComparison.OrdinalIgnoreCase)]
    public void ThrowIfInvalidStringComparison_WhenValueIsCanonical_ShouldNotThrowAndReportNothing(string testName, StringComparison comparison) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfInvalidStringComparison(comparison, "comparisonType"), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfInvalidStringComparison" /> throws
    /// <see cref="ArgumentException" /> with <c>ParamName == "comparisonType"</c> for undefined enum values
    /// at boundary positions.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="comparison">The <see cref="StringComparison" /> passed to the guard.</param>
    [TestMethod]
    [DataRow("undefined 999", (StringComparison)999)]
    [DataRow("undefined -1", (StringComparison)(-1))]
    [DataRow("undefined MaxValue", (StringComparison)int.MaxValue)]
    public void ThrowIfInvalidStringComparison_WhenValueIsUndefined_ShouldThrowOnComparisonType(string testName, StringComparison comparison) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfInvalidStringComparison(comparison, "comparisonType"),
            typeof(ArgumentException),
            "comparisonType");

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfInvalidStringComparison" />, when ValueIsInvalid, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow((StringComparison)999)]
    [DataRow((StringComparison)(-1))]
    [DataRow((StringComparison)int.MaxValue)]
    public void ThrowIfInvalidStringComparison_WhenValueIsInvalid_ShouldThrowExactly(StringComparison stringComparison)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfInvalidStringComparison(stringComparison);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfInvalidStringComparison" />, when ValueIsValid, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(StringComparison.CurrentCulture)]
    [DataRow(StringComparison.CurrentCultureIgnoreCase)]
    [DataRow(StringComparison.InvariantCulture)]
    [DataRow(StringComparison.InvariantCultureIgnoreCase)]
    [DataRow(StringComparison.Ordinal)]
    [DataRow(StringComparison.OrdinalIgnoreCase)]
    public void ThrowIfInvalidStringComparison_WhenValueIsValid_ShouldNotThrow(StringComparison stringComparison) => ThrowHelper.ThrowIfInvalidStringComparison(stringComparison);

}
