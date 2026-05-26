// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KatDisplayNameTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Test.Kat;

/// <summary>
/// Verifies the behaviour of <see cref="KatDisplayName.GetDisplayName" /> across the row shapes it must
/// handle: a single <see cref="IKat" /> payload, a non-KAT payload, an empty payload, and a null
/// <see cref="MethodInfo" />.
/// </summary>
[TestClass]
public sealed class KatDisplayNameTests
{
    /// <summary>
    /// Verifies that <see cref="KatDisplayName.GetDisplayName" /> returns
    /// <c>"{methodName}({kat.Name})"</c> when the first row element is an <see cref="IKat" /> instance.
    /// </summary>
    [TestMethod]
    public void GetDisplayName_WhenFirstElementIsKat_ShouldReturnNamedRow()
    {
        MethodInfo method = typeof(KatDisplayNameTests).GetMethod(nameof(GetDisplayName_WhenFirstElementIsKat_ShouldReturnNamedRow))!;
        BinaryKat<int, bool> kat = new("sample-row", 1, true);

        string actual = KatDisplayName.GetDisplayName(method, new object?[] { kat });

        Assert.AreEqual($"{method.Name}(sample-row)", actual);
    }

    /// <summary>
    /// Verifies that <see cref="KatDisplayName.GetDisplayName" /> returns the unadorned method name when
    /// the first row element is not an <see cref="IKat" />.
    /// </summary>
    [TestMethod]
    public void GetDisplayName_WhenFirstElementIsNotKat_ShouldReturnMethodName()
    {
        MethodInfo method = typeof(KatDisplayNameTests).GetMethod(nameof(GetDisplayName_WhenFirstElementIsNotKat_ShouldReturnMethodName))!;

        string actual = KatDisplayName.GetDisplayName(method, new object?[] { 42, "not-a-kat" });

        Assert.AreEqual(method.Name, actual);
    }

    /// <summary>
    /// Verifies that <see cref="KatDisplayName.GetDisplayName" /> returns the unadorned method name when
    /// the row payload is empty.
    /// </summary>
    [TestMethod]
    public void GetDisplayName_WhenDataIsEmpty_ShouldReturnMethodName()
    {
        MethodInfo method = typeof(KatDisplayNameTests).GetMethod(nameof(GetDisplayName_WhenDataIsEmpty_ShouldReturnMethodName))!;

        string actual = KatDisplayName.GetDisplayName(method, Array.Empty<object?>());

        Assert.AreEqual(method.Name, actual);
    }

    /// <summary>
    /// Verifies that <see cref="KatDisplayName.GetDisplayName" /> throws
    /// <see cref="ArgumentNullException" /> when <c>methodInfo</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetDisplayName_WhenMethodInfoIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = KatDisplayName.GetDisplayName(null!, new object?[] { new BinaryKat<int, bool>("row", 0, false) });
        });
    }
}
