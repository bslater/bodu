// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OlePropertyValueTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

/// <summary>
/// Verifies the typed accessors of <see cref="OlePropertyValue" />.
/// </summary>
[TestClass]
public partial class OlePropertyValueTests
{
    /// <summary>
    /// Verifies that an integral value is returned by the integer accessors and rejected by the string accessor.
    /// </summary>
    [TestMethod]
    public void AsInt32_WhenValueIsInt32_ShouldReturnValue()
    {
        OlePropertyValue value = new() { Type = OlePropertyType.Int32, Value = 42 };

        Assert.AreEqual(42, value.AsInt32());
        Assert.AreEqual(42L, value.AsInt64());
        Assert.IsNull(value.AsString());
    }

    /// <summary>
    /// Verifies that a string value is returned by the string accessor.
    /// </summary>
    [TestMethod]
    public void AsString_WhenValueIsString_ShouldReturnValue()
    {
        OlePropertyValue value = new() { Type = OlePropertyType.AnsiString, Value = "hello" };

        Assert.AreEqual("hello", value.AsString());
        Assert.IsNull(value.AsInt32());
    }

    /// <summary>
    /// Verifies that a FILETIME value can be read both as a point in time and as an elapsed duration.
    /// </summary>
    [TestMethod]
    public void FileTime_WhenValueIsTicks_ShouldReadAsDateAndDuration()
    {
        long ticks = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToFileTimeUtc();
        OlePropertyValue value = new() { Type = OlePropertyType.FileTime, Value = ticks };

        Assert.AreEqual(2001, value.AsDateTimeOffset()!.Value.Year);
        Assert.AreEqual(TimeSpan.FromTicks(ticks), value.AsTimeSpan());
    }

    /// <summary>
    /// Verifies that a binary value is returned by the byte accessor.
    /// </summary>
    [TestMethod]
    public void AsBytes_WhenValueIsBlob_ShouldReturnArray()
    {
        byte[] payload = [1, 2, 3];
        OlePropertyValue value = new() { Type = OlePropertyType.Blob, Value = payload };

        CollectionAssert.AreEqual(payload, value.AsBytes());
    }

    /// <summary>
    /// Verifies that <see cref="OlePropertyValue.GetValueOrDefault{T}" /> returns the typed value or the default.
    /// </summary>
    [TestMethod]
    public void GetValueOrDefault_WhenTypeMatchesOrNot_ShouldReturnValueOrDefault()
    {
        OlePropertyValue value = new() { Type = OlePropertyType.Int32, Value = 7 };

        Assert.AreEqual(7, value.GetValueOrDefault<int>());
        Assert.AreEqual(0d, value.GetValueOrDefault<double>());
    }

    /// <summary>
    /// Verifies that a vector value exposes its element array.
    /// </summary>
    [TestMethod]
    public void IsVector_WhenValueIsVector_ShouldExposeElements()
    {
        OlePropertyValue value = new() { Type = OlePropertyType.AnsiString, IsVector = true, Value = new object?[] { "a", "b" } };

        Assert.IsTrue(value.IsVector);
        Assert.HasCount(2, (object?[])value.Value!);
    }
}
