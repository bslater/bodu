// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.TryEncodeTo.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// Provides destination-size variations for <see cref="TryEncodeTo_ShouldRespectDestinationSize" /> covering
    /// exactly-sized, oversized, and undersized buffers.
    /// </summary>
    /// <returns>The test cases.</returns>
    public static IEnumerable<object[]> GetTryEncodeToCases() =>
    [
        [0, true],
        [1, true],
        [-1, false],
    ];

    /// <summary>
    /// Verifies that
    /// <see cref="StringEncodingExtensions.TryEncodeTo(string, System.Text.Encoding, Span{byte}, out int)" />
    /// returns <see langword="true" /> with the correct count when the destination fits, and
    /// <see langword="false" /> with zero when it does not.
    /// </summary>
    /// <param name="extra">Extra capacity added to the required buffer length (negative to undersize).</param>
    /// <param name="expectedOk">Whether the call is expected to succeed.</param>
    [DataTestMethod]
    [DynamicData(nameof(GetTryEncodeToCases), DynamicDataSourceType.Method)]
    public void TryEncodeTo_ShouldRespectDestinationSize(int extra, bool expectedOk)
    {
        var required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        var backing = new byte[required + extra];

        var ok = MultiByteText.TryEncodeTo(System.Text.Encoding.UTF8, backing, out var written);

        Assert.AreEqual(expectedOk, ok);
        Assert.AreEqual(expectedOk ? required : 0, written);
    }

    /// <summary>
    /// Verifies that
    /// <see cref="StringEncodingExtensions.TryEncodeTo(string, System.Text.Encoding, Span{byte}, out int)" />
    /// throws <see cref="ArgumentNullException" /> when <c>text</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryEncodeTo_WhenTextIsNull_ShouldThrowExactly()
    {
        var backing = new byte[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringEncodingExtensions.TryEncodeTo(null!, System.Text.Encoding.UTF8, backing, out _);
        });

        Assert.AreEqual("text", ex.ParamName);
    }

    /// <summary>
    /// Verifies that
    /// <see cref="StringEncodingExtensions.TryEncodeTo(string, System.Text.Encoding, Span{byte}, out int)" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void TryEncodeTo_WhenEncodingIsNull_ShouldThrowExactly()
    {
        var backing = new byte[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SampleText.TryEncodeTo(null!, backing, out _);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
