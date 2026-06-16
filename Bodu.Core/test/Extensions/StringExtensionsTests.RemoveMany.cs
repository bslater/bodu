// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.RemoveMany.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveMany(string, string[])" /> removes every supplied
    /// substring.
    /// </summary>
    [TestMethod]
    public void RemoveMany_WhenMultipleValuesSupplied_ShouldRemoveEach()
    {
        string actual = "Hello, World!".RemoveMany(",", "!", "o");

        Assert.AreEqual("Hell Wrld", actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveMany(string, string[])" /> returns the original
    /// instance when no values are supplied.
    /// </summary>
    [TestMethod]
    public void RemoveMany_WhenArrayIsEmpty_ShouldReturnSameInstance()
    {
        string value = "hello";

        string actual = value.RemoveMany();

        Assert.AreSame(value, actual);
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveMany(string, string[])" /> throws
    /// <see cref="ArgumentException" /> when any entry is empty.
    /// </summary>
    [TestMethod]
    public void RemoveMany_WhenAnyEntryIsEmpty_ShouldThrowExactly() => Assert.ThrowsExactly<ArgumentException>(() => _ = "hello".RemoveMany("x", string.Empty));

    /// <summary>
    /// Verifies that <see cref="StringExtensions.RemoveMany(string, string[])" /> throws
    /// <see cref="ArgumentNullException" /> for null arguments.
    /// </summary>
    [TestMethod]
    public void RemoveMany_WhenAnyArgumentIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = StringExtensions.RemoveMany(null!, "x"));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".RemoveMany((string[])null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = "hello".RemoveMany("x", null!));
    }
}
