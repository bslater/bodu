// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotOfType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" />, when IntValueIsString, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotOfType_WhenIntValueIsString_ShouldThrowExactly()
    {
        object value = 42;

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfNotOfType<string>(value);
        });
    }
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType{T}" /> for the <see cref="int" /> instantiation
    /// does not throw — and on the ParamName-asserting overload reports nothing — for an int instance.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value passed to the guard.</param>
    [TestMethod]
    [DataRow("int value against int", 42)]
    public void ThrowIfNotOfType_WhenValueIsAccepted_ShouldNotThrowAndReportNothing(string testName, object value) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfNotOfType<int>(value, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType{T}" /> for the <see cref="int" /> instantiation
    /// throws <see cref="ArgumentException" /> with <c>ParamName == "value"</c> for instances of any other
    /// type.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value passed to the guard.</param>
    [TestMethod]
    [DataRow("string value against int", "string")]
    [DataRow("long value against int", 42L)]
    public void ThrowIfNotOfType_WhenValueIsRejected_ShouldThrowOnValue(string testName, object value) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfNotOfType<int>(value, nameof(value)), typeof(ArgumentException), "value");

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" />, when NullValueAndTargetIsNonNullable, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotOfType_WhenNullValueAndTargetIsNonNullable_ShouldThrowExactly()
    {
        object? value = null;

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfNotOfType<int>(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" />, when StringValueIsNotInt, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotOfType_WhenStringValueIsNotInt_ShouldThrowExactly()
    {
        object value = "string";

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfNotOfType<int>(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" /> on a <see langword="null" /> input bound to a
    /// non-nullable target throws <see cref="ArgumentException" /> with ParamName "value".
    /// </summary>
    [TestMethod]
    public void ThrowIfNotOfType_WhenValueIsNullAgainstNonNullableTarget_ShouldThrowExactly()
    {
        AssertGuard(
            "null against int → ArgumentException",
            () => ThrowHelper.ThrowIfNotOfType<int>(null, "value"),
            typeof(ArgumentException),
            "value");
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" />, when ValueIsNullNullableValueType, NotThrow.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotOfType_WhenValueIsNullNullableValueType_ShouldNotThrow()
    {
        object? value = null;
        ThrowHelper.ThrowIfNotOfType<int?>(value);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" />, when ValueIsNullReferenceType, NotThrow.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotOfType_WhenValueIsNullReferenceType_ShouldNotThrow()
    {
        object? value = null;
        ThrowHelper.ThrowIfNotOfType<string>(value);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" />, when ValueIsOfExpectedType, NotThrow.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotOfType_WhenValueIsOfExpectedType_ShouldNotThrow()
    {
        object value = 42;
        ThrowHelper.ThrowIfNotOfType<int>(value);
    }

}
