// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandardTests.Ctors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public partial class CrcStandardTests
{

    /// <summary>
    /// Verifies that passing an empty string as the CRC name to the constructor throws
    /// <see cref="ArgumentException" /> with <c>ParamName</c> equal to <c>name</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNameIsEmpty_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(
            () => new CrcStandard(string.Empty, 32, 0x04C11DB7UL, 0xFFFFFFFFUL, true, true, 0xFFFFFFFFUL));
        Assert.AreEqual("name", ex.ParamName);
    }
    /// <summary>
    /// Verifies that passing <see langword="null" /> as the CRC name to the constructor throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> equal to <c>name</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNameIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(
            () => new CrcStandard(null!, 32, 0x04C11DB7UL, 0xFFFFFFFFUL, true, true, 0xFFFFFFFFUL));
        Assert.AreEqual("name", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a successfully constructed <see cref="CrcStandard" /> returns the supplied parameter values
    /// through every init-only property.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenParametersAreValid_ShouldRoundTripAllInitOnlyValues()
    {
        CrcStandard standard = new(
            name: "Round-Trip",
            size: 24,
            polynomial: 0x864CFBUL,
            initialValue: 0xB704CEUL,
            reflectIn: false,
            reflectOut: true,
            xOrOut: 0xDEADBEEFUL);

        Assert.AreEqual("Round-Trip", standard.Name);
        Assert.AreEqual(24, standard.Size);
        Assert.AreEqual(0x864CFBUL, standard.Polynomial);
        Assert.AreEqual(0xB704CEUL, standard.InitialValue);
        Assert.IsFalse(standard.ReflectIn);
        Assert.IsTrue(standard.ReflectOut);
        Assert.AreEqual(0xDEADBEEFUL, standard.XOrOut);
    }

    /// <summary>
    /// Verifies that constructing a <see cref="CrcStandard" /> with a <paramref name="size" /> outside the
    /// inclusive range [<see cref="CrcStandard.MinSize" />, <see cref="CrcStandard.MaxSize" />] throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName</c> equal to <c>size</c>.
    /// </summary>
    /// <param name="size">The invalid width under test.</param>
    [TestMethod]
    [DataRow(int.MinValue)]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(65)]
    [DataRow(128)]
    [DataRow(int.MaxValue)]
    public void Ctor_WhenSizeIsOutOfRange_ShouldThrowExactly(int size)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new CrcStandard("Test", size, 0x1UL, 0x0UL, false, false, 0x0UL));
        Assert.AreEqual("size", ex.ParamName);
    }

}
