// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfEnumValueIsUndefined.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfEnumValueIsUndefined{TEnum}" /> contract matrix with
    /// explicit ParamName assertions: undefined enum values throw <see cref="ArgumentOutOfRangeException" />
    /// with the expected ParamName; defined values pass.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The enum value passed to the guard.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("defined A → pass", TestEnum.A, false)]
    [DataRow("defined B → pass", TestEnum.B, false)]
    [DataRow("undefined 99 → throw", (TestEnum)99, true)]
    [DataRow("undefined -1 → throw", (TestEnum)(-1), true)]
    public void ThrowIfEnumValueIsUndefined_WhenInvokedWithVariousValues_ShouldFollowContract(
        string testName, TestEnum value, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(testName, () => ThrowHelper.ThrowIfEnumValueIsUndefined(value, nameof(value)), expected, expectedParam);
    }

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
    public void ThrowIfEnumValueIsUndefined_WhenValueIsDefined_ShouldNotThrow(TestEnum value) => ThrowHelper.ThrowIfEnumValueIsUndefined(value);
}
