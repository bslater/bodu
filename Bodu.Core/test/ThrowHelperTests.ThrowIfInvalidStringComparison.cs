// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfInvalidStringComparison.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
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
    public void ThrowIfInvalidStringComparison_WhenValueIsValid_ShouldNotThrow(StringComparison stringComparison)
    {
        ThrowHelper.ThrowIfInvalidStringComparison(stringComparison);
    }
}
