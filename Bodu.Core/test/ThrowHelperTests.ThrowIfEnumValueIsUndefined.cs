// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfEnumValueIsUndefined.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEnumValueIsUndefined{TEnum}" /> does not throw — and on
    /// the ParamName-asserting overload reports nothing — for defined enum values.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The enum value passed to the guard.</param>
    [TestMethod]
    [DataRow("defined A", TestEnum.A)]
    [DataRow("defined B", TestEnum.B)]
    public void ThrowIfEnumValueIsUndefined_WhenValueIsDefined_ShouldNotThrowAndReportNothing(string testName, TestEnum value) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfEnumValueIsUndefined(value, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEnumValueIsUndefined{TEnum}" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for undefined enum values.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The enum value passed to the guard.</param>
    [TestMethod]
    [DataRow("undefined 99", (TestEnum)99)]
    [DataRow("undefined -1", (TestEnum)(-1))]
    public void ThrowIfEnumValueIsUndefined_WhenValueIsUndefined_ShouldThrowOnValue(string testName, TestEnum value) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfEnumValueIsUndefined(value, nameof(value)),
            typeof(ArgumentOutOfRangeException),
            "value");

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEnumValueIsUndefined" />, when ValueIsDefined, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(TestEnum.A)]
    [DataRow(TestEnum.B)]
    public void ThrowIfEnumValueIsUndefined_WhenValueIsDefined_ShouldNotThrow(TestEnum value) => ThrowHelper.ThrowIfEnumValueIsUndefined(value);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfEnumValueIsUndefined" />, when ValueIsUndefined, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow((TestEnum)99)]
    [DataRow((TestEnum)(-1))]
    public void ThrowIfEnumValueIsUndefined_WhenValueIsUndefined_ShouldThrowExactly(TestEnum value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfEnumValueIsUndefined(value);
        });
    }

}
