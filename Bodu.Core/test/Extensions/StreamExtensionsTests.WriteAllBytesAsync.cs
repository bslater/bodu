// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamExtensionsTests.WriteAllBytesAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StreamExtensionsTests
{
    /// <summary>
    /// Verifies that <c>WriteAllBytesAsync</c> writes the entire buffer to the stream.
    /// </summary>
    [TestMethod]
    public async Task WriteAllBytesAsync_WhenInvoked_ShouldWriteAllBytes()
    {
        byte[] content = [1, 2, 3, 4, 5];
        using MemoryStream stream = new();

        await stream.WriteAllBytesAsync(content);

        CollectionAssert.AreEqual(content, stream.ToArray());
    }

    /// <summary>
    /// Verifies that <c>WriteAllBytesAsync</c> throws <see cref="ArgumentNullException" /> synchronously when the
    /// stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WriteAllBytesAsync_WhenStreamIsNull_ShouldThrowExactly()
    {
        Stream stream = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = stream.WriteAllBytesAsync(new byte[] { 1, 2, 3 });
        });
    }
}
