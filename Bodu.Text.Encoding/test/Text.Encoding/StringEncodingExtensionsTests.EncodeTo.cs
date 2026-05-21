// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringEncodingExtensionsTests.EncodeTo.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class StringEncodingExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.EncodeTo(string, System.Text.Encoding, Span{byte})" />
    /// writes the encoded bytes and returns the correct count when the destination fits.
    /// </summary>
    [TestMethod]
    public void EncodeTo_WhenDestinationFits_ShouldWriteAndReturnCount()
    {
        var required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        Span<byte> destination = new byte[required];

        var written = MultiByteText.EncodeTo(System.Text.Encoding.UTF8, destination);

        Assert.AreEqual(required, written);
        CollectionAssert.AreEqual(
            System.Text.Encoding.UTF8.GetBytes(MultiByteText),
            destination.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.EncodeTo(string, System.Text.Encoding, Span{byte})" />
    /// throws <see cref="ArgumentException" /> when the destination is one byte too small.
    /// </summary>
    [TestMethod]
    public void EncodeTo_WhenDestinationIsOneByteTooSmall_ShouldThrowArgumentException()
    {
        var required = System.Text.Encoding.UTF8.GetByteCount(MultiByteText);
        var backing = new byte[required - 1];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = MultiByteText.EncodeTo(System.Text.Encoding.UTF8, backing);
        });
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.EncodeTo(string, System.Text.Encoding, Span{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <c>text</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EncodeTo_WhenTextIsNull_ShouldThrowArgumentNullException()
    {
        var backing = new byte[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = StringEncodingExtensions.EncodeTo(null!, System.Text.Encoding.UTF8, backing);
        });

        Assert.AreEqual("text", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="StringEncodingExtensions.EncodeTo(string, System.Text.Encoding, Span{byte})" />
    /// throws <see cref="ArgumentNullException" /> when <c>encoding</c> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void EncodeTo_WhenEncodingIsNull_ShouldThrowArgumentNullException()
    {
        var backing = new byte[64];

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = SampleText.EncodeTo(null!, backing);
        });

        Assert.AreEqual("encoding", ex.ParamName);
    }
}
