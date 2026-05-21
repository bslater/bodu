// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfInvalidStringComparison.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfInvalidStringComparison" /> contract with explicit ParamName
    /// assertions: undefined <see cref="StringComparison" /> values throw on "comparisonType"; the six
    /// canonical values pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="comparison">The <see cref="StringComparison" /> passed to the guard.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("CurrentCulture → pass", StringComparison.CurrentCulture, false)]
    [DataRow("CurrentCultureIgnoreCase → pass", StringComparison.CurrentCultureIgnoreCase, false)]
    [DataRow("InvariantCulture → pass", StringComparison.InvariantCulture, false)]
    [DataRow("InvariantCultureIgnoreCase → pass", StringComparison.InvariantCultureIgnoreCase, false)]
    [DataRow("Ordinal → pass", StringComparison.Ordinal, false)]
    [DataRow("OrdinalIgnoreCase → pass", StringComparison.OrdinalIgnoreCase, false)]
    [DataRow("undefined 999 → throw on comparisonType", (StringComparison)999, true)]
    [DataRow("undefined -1 → throw on comparisonType", (StringComparison)(-1), true)]
    [DataRow("undefined MaxValue → throw on comparisonType", (StringComparison)int.MaxValue, true)]
    public void ThrowIfInvalidStringComparison_WhenInvokedWithVariousValues_ShouldFollowContract(
        string testName, StringComparison comparison, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentException) : null;
        var expectedParam = expectsException ? "comparisonType" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfInvalidStringComparison(comparison, "comparisonType"),
            expected,
            expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfInvalidStringComparison" />, when ValueIsInvalid, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow((StringComparison)999)]
    [DataRow((StringComparison)(-1))]
    [DataRow((StringComparison)int.MaxValue)]
    public void ThrowIfInvalidStringComparison_WhenValueIsInvalid_ShouldThrowArgumentException(StringComparison stringComparison)
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
