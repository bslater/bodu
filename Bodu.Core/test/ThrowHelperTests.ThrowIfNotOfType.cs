// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNotOfType.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" />, when IntValueIsString, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotOfType_WhenIntValueIsString_ShouldThrowException()
    {
        object value = 42;

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfNotOfType<string>(value);
        });
    }
    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfNotOfType{T}" /> contract for the <see cref="int" />
    /// instantiation with explicit ParamName assertions: a non-int instance or a <see langword="null" />
    /// against the non-nullable target throws <see cref="ArgumentException" /> with ParamName "value"; an int
    /// instance passes.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value passed to the guard.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("string value against int → throw", "string", true)]
    [DataRow("int value against int → pass", 42, false)]
    [DataRow("long value against int → throw", 42L, true)]
    public void ThrowIfNotOfType_WhenInvokedWithVariousValues_ShouldFollowContract(
        string testName, object value, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(testName, () => ThrowHelper.ThrowIfNotOfType<int>(value, nameof(value)), expected, expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNotOfType" />, when NullValueAndTargetIsNonNullable, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfNotOfType_WhenNullValueAndTargetIsNonNullable_ShouldThrowException()
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
    public void ThrowIfNotOfType_WhenStringValueIsNotInt_ShouldThrowException()
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
    public void ThrowIfNotOfType_WhenValueIsNullAgainstNonNullableTarget_ShouldThrowWithParamName()
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
