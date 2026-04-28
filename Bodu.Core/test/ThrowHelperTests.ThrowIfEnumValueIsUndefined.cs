// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfEnumValueIsUndefined.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEnumValueIsUndefined" />, when ValueIsUndefined, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow((TestEnum)99)]
    [DataRow((TestEnum)(-1))]
    public void ThrowIfEnumValueIsUndefined_WhenValueIsUndefined_ShouldThrowArgumentOutOfRangeException(TestEnum value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEnumValueIsUndefined" />, when ValueIsDefined, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(TestEnum.A)]
    [DataRow(TestEnum.B)]
    public void ThrowIfEnumValueIsUndefined_WhenValueIsDefined_ShouldNotThrow(TestEnum value)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(value);
    }
}
