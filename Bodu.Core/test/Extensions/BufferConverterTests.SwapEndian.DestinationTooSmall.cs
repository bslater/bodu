// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferConverterTests.SwapEndian.DestinationTooSmall.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class BufferConverterTests
{
    /// <summary>
    /// Verifies that <see cref="BufferConverter.SwapEndian(System.ReadOnlySpan{byte}, System.Span{byte}, int)" /> throws when the
    /// destination is shorter than the source — covering the conditional throw at line 143.
    /// </summary>
    [TestMethod]
    public void SwapEndian_ByteSpans_WhenDestinationIsShorterThanSource_ShouldThrowExactly()
    {
        var source = new byte[8];   // 8 bytes
        var destination = new byte[4]; // half the size, still a positive multiple of elementSize=2

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            BufferConverter.SwapEndian(source, destination, 2);
        });
    }
}
