// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfBitsOutsideMask.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfBitsOutsideMask{T}" /> does not throw, and on the
    /// ParamName-asserting overload reports nothing, for values whose bits all lie inside the mask, contiguous or not.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value passed to the guard.</param>
    /// <param name="mask">The mask passed to the guard.</param>
    [TestMethod]
    [DataRow("zero in a seven-bit mask", 0UL, 0x7FUL)]
    [DataRow("the whole seven-bit mask", 0x7FUL, 0x7FUL)]
    [DataRow("alternate bits of a seven-bit mask", 0x55UL, 0x7FUL)]
    [DataRow("two bits of a mask with gaps", 0x82UL, 0xAAUL)]
    [DataRow("the whole mask with gaps", 0xAAUL, 0xAAUL)]
    [DataRow("every bit in the full mask", ulong.MaxValue, ulong.MaxValue)]
    [DataRow("zero in an empty mask", 0UL, 0UL)]
    public void ThrowIfBitsOutsideMask_WhenBitsAreInsideTheMask_ShouldNotThrowAndReportNothing(string testName, ulong value, ulong mask) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfBitsOutsideMask(value, mask, nameof(value)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfBitsOutsideMask{T}" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "value"</c> for a value that sets a bit outside
    /// the mask, including a bit in a gap of the mask below its highest bit, which a comparison with the mask's
    /// largest value would accept.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value passed to the guard.</param>
    /// <param name="mask">The mask passed to the guard.</param>
    [TestMethod]
    [DataRow("the bit above a seven-bit mask", 0x80UL, 0x7FUL)]
    [DataRow("the top bit beside a seven-bit mask", 0x8000_0000_0000_0001UL, 0x7FUL)]
    [DataRow("a bit in a gap of the mask", 0x04UL, 0xAAUL)]
    [DataRow("the lowest bit, outside the mask", 0x01UL, 0xAAUL)]
    [DataRow("every bit, against a mask with gaps", ulong.MaxValue, 0xAAUL)]
    [DataRow("any bit, against an empty mask", 0x01UL, 0UL)]
    public void ThrowIfBitsOutsideMask_WhenABitIsOutsideTheMask_ShouldThrowOnValue(string testName, ulong value, ulong mask) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfBitsOutsideMask(value, mask, nameof(value)),
            typeof(ArgumentOutOfRangeException),
            "value");

    /// <summary>
    /// Verifies that the message of the exception <see cref="ThrowHelper.ThrowIfBitsOutsideMask{T}" /> throws names
    /// the bits outside the mask and the mask, in hexadecimal.
    /// </summary>
    [TestMethod]
    public void ThrowIfBitsOutsideMask_WhenABitIsOutsideTheMask_ShouldNameTheBitsAndTheMask()
    {
        ulong bits = 0x86;

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfBitsOutsideMask(bits, 0xAAUL);
        });

        Assert.AreEqual("bits", ex.ParamName);
        Assert.IsTrue(ex.Message.Contains("0x4", StringComparison.Ordinal), ex.Message);
        Assert.IsTrue(ex.Message.Contains("0xAA", StringComparison.Ordinal), ex.Message);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfBitsOutsideMask{T}" /> works over a narrower and a signed integer
    /// type, through its <c>IBinaryInteger&lt;T&gt;</c> constraint.
    /// </summary>
    [TestMethod]
    public void ThrowIfBitsOutsideMask_WhenTheTypeIsNotUInt64_ShouldTestTheBitsOfThatType()
    {
        ThrowHelper.ThrowIfBitsOutsideMask((byte)0x0F, (byte)0x0F);
        ThrowHelper.ThrowIfBitsOutsideMask(-1, -1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfBitsOutsideMask((byte)0x10, (byte)0x0F);
        });
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfBitsOutsideMask(-1, int.MaxValue);
        });
    }
}
