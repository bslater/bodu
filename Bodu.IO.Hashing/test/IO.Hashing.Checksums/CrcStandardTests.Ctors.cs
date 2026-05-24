// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcStandardTests.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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

    /// <summary>
    /// Verifies that constructing a <see cref="CrcStandard" /> with a <paramref name="polynomial" /> that exceeds the
    /// <paramref name="size" />-bit width mask throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> equal to <c>polynomial</c>.
    /// </summary>
    /// <param name="size">The CRC width, in bits.</param>
    /// <param name="polynomial">A polynomial value that does not fit in <paramref name="size" /> bits.</param>
    [TestMethod]
    [DataRow(1, 0x2UL)]
    [DataRow(3, 0x10UL)]
    [DataRow(8, 0x100UL)]
    [DataRow(8, 0x107UL)]
    [DataRow(16, 0x10000UL)]
    [DataRow(32, 0x1_0000_0000UL)]
    [DataRow(63, 0x8000_0000_0000_0000UL)]
    public void Ctor_WhenPolynomialExceedsWidth_ShouldThrowExactly(int size, ulong polynomial)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new CrcStandard("Test", size, polynomial, 0x0UL, false, false, 0x0UL));
        Assert.AreEqual("polynomial", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructing a <see cref="CrcStandard" /> with an <paramref name="initialValue" /> that exceeds
    /// the <paramref name="size" />-bit width mask throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> equal to <c>initialValue</c>.
    /// </summary>
    /// <param name="size">The CRC width, in bits.</param>
    /// <param name="initialValue">An initial register value that does not fit in <paramref name="size" /> bits.</param>
    [TestMethod]
    [DataRow(1, 0x2UL)]
    [DataRow(8, 0x100UL)]
    [DataRow(16, 0x10000UL)]
    [DataRow(32, 0x1_0000_0000UL)]
    public void Ctor_WhenInitialValueExceedsWidth_ShouldThrowExactly(int size, ulong initialValue)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new CrcStandard("Test", size, 0x1UL, initialValue, false, false, 0x0UL));
        Assert.AreEqual("initialValue", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructing a <see cref="CrcStandard" /> with an <paramref name="xOrOut" /> that exceeds the
    /// <paramref name="size" />-bit width mask throws <see cref="ArgumentOutOfRangeException" /> with
    /// <c>ParamName</c> equal to <c>xOrOut</c>.
    /// </summary>
    /// <param name="size">The CRC width, in bits.</param>
    /// <param name="xOrOut">A final-XOR value that does not fit in <paramref name="size" /> bits.</param>
    [TestMethod]
    [DataRow(1, 0x2UL)]
    [DataRow(8, 0x100UL)]
    [DataRow(16, 0x10000UL)]
    [DataRow(32, 0x1_0000_0000UL)]
    public void Ctor_WhenXOrOutExceedsWidth_ShouldThrowExactly(int size, ulong xOrOut)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new CrcStandard("Test", size, 0x1UL, 0x0UL, false, false, xOrOut));
        Assert.AreEqual("xOrOut", ex.ParamName);
    }

    /// <summary>
    /// Verifies that constructing a <see cref="CrcStandard" /> with parameters that exactly fill the width mask
    /// succeeds — the upper boundary of the permitted range.
    /// </summary>
    /// <param name="size">The CRC width, in bits.</param>
    /// <param name="widthMask">The all-ones value for <paramref name="size" /> bits.</param>
    [TestMethod]
    [DataRow(1, 0x1UL)]
    [DataRow(8, 0xFFUL)]
    [DataRow(16, 0xFFFFUL)]
    [DataRow(32, 0xFFFFFFFFUL)]
    [DataRow(64, ulong.MaxValue)]
    public void Ctor_WhenParametersFillWidthMask_ShouldNotThrow(int size, ulong widthMask)
    {
        CrcStandard standard = new("Test", size, widthMask, widthMask, false, false, widthMask);

        Assert.AreEqual(widthMask, standard.Polynomial);
        Assert.AreEqual(widthMask, standard.InitialValue);
        Assert.AreEqual(widthMask, standard.XOrOut);
    }

}
