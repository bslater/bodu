// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.TryEncodeUtf8To.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// Provides destination-size variations for <see cref="TryEncodeUtf8To_ShouldRespectDestinationSize" />
    /// covering exactly-sized, oversized, and undersized buffers.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object[]> GetTryEncodeUtf8ToCases() =>
    [
        [0, true],
        [1, true],
        [-1, false],
    ];

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.TryEncodeUtf8To(string, Span{byte}, out int)" />
    /// returns <see langword="true" /> with the correct count when the destination fits, and
    /// <see langword="false" /> with zero when it does not.
    /// </summary>
    /// <param name="extra">Extra capacity added to the required buffer length (negative to undersize).</param>
    /// <param name="expectedOk">Whether the call is expected to succeed.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetTryEncodeUtf8ToCases), DynamicDataSourceType.Method)]
    public void TryEncodeUtf8To_ShouldRespectDestinationSize(int extra, bool expectedOk)
    {
        var required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        var backing = new byte[required + extra];

        var ok = MultiByteText.TryEncodeUtf8To(backing, out var written);

        Assert.AreEqual(expectedOk, ok);
        Assert.AreEqual(expectedOk ? required : 0, written);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.TryEncodeUtf8To(string, Span{byte}, out int)" />
    /// throws <see cref="ArgumentNullException" /> when <c>text</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryEncodeUtf8To_WhenTextIsNull_ShouldThrowExactly()
    {
        var backing = new byte[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringEncodingExtensions.TryEncodeUtf8To(null!, backing, out _);
        });

        Assert.AreEqual("text", ex.ParamName);
    }
}
