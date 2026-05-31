// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedStringTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Behavioural tests for <see cref="BencodedString" />. Partial files split coverage by member or subject.
/// </summary>
[TestClass]
public sealed partial class BencodedStringTests
{

    /// <summary>
    /// Verifies that the byte-array constructor rejects a <see langword="null" /> argument with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenByteArrayIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new BencodedString((byte[])null!);
        });

        Assert.AreEqual("bytes", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the byte-array constructor stores a defensive copy of the supplied array.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenByteArrayProvided_ShouldStoreCopy()
    {
        byte[] source = [0xDE, 0xAD, 0xBE, 0xEF];

        BencodedString value = new(source);

        source[0] = 0x00;

        Assert.AreEqual(0xDE, value.Bytes.Span[0]);
    }
    /// <summary>
    /// Verifies that the span constructor copies the supplied bytes into the new instance and exposes them via
    /// <see cref="BencodedString.Bytes" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSpanProvided_ShouldStoreCopyOfBytes()
    {
        ReadOnlySpan<byte> source = stackalloc byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

        BencodedString value = new(source);

        CollectionAssert.AreEqual(source.ToArray(), value.Bytes.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BencodedValue.Kind" /> returns <see cref="BencodedValueKind.String" />.
    /// </summary>
    [TestMethod]
    public void Kind_ShouldReturnString()
    {
        BencodedString value = new(Array.Empty<byte>());

        Assert.AreEqual(BencodedValueKind.String, value.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedString.Length" /> reports the byte count exposed by
    /// <see cref="BencodedString.Bytes" />.
    /// </summary>
    [TestMethod]
    public void Length_WhenBytesProvided_ShouldReturnByteCount()
    {
        BencodedString value = new([1, 2, 3, 4, 5]);

        Assert.AreEqual(5, value.Length);
    }

}
