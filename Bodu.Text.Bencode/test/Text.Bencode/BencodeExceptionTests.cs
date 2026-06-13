// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeExceptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Verifies the constructor surface and position-reporting contracts of <see cref="BencodeFormatException" /> and
/// <see cref="BencodeSerializationException" />, and their placement in the exception hierarchy.
/// </summary>
[TestClass]
public class BencodeExceptionTests
{
    /// <summary>
    /// Verifies that a default-constructed <see cref="BencodeFormatException" /> carries no offset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ForBencodeFormatException_ShouldLeaveOffsetNull()
    {
        var ex = new BencodeFormatException();

        Assert.IsNull(ex.Offset);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that the message constructor of <see cref="BencodeFormatException" /> preserves the message and
    /// leaves the offset unset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageProvided_ForBencodeFormatException_ShouldPreserveMessage()
    {
        var ex = new BencodeFormatException("malformed");

        Assert.AreEqual("malformed", ex.Message);
        Assert.IsNull(ex.Offset);
    }

    /// <summary>
    /// Verifies that the inner-exception constructor of <see cref="BencodeFormatException" /> preserves the chained
    /// exception.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInnerExceptionProvided_ForBencodeFormatException_ShouldPreserveChain()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new BencodeFormatException("outer", inner);

        Assert.AreEqual("outer", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
    }

    /// <summary>
    /// Verifies that the offset constructor of <see cref="BencodeFormatException" /> records the byte offset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOffsetProvided_ForBencodeFormatException_ShouldRecordOffset()
    {
        var ex = new BencodeFormatException("malformed", 17);

        Assert.AreEqual("malformed", ex.Message);
        Assert.AreEqual(17, ex.Offset);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeFormatException" /> derives from <see cref="FormatException" />, so callers
    /// can catch the framework base type.
    /// </summary>
    [TestMethod]
    public void BencodeFormatException_WhenCaughtAsFormatException_ShouldMatchHierarchy()
    {
        var ex = new BencodeFormatException();

        Assert.IsInstanceOfType<FormatException>(ex);
    }

    /// <summary>
    /// Verifies that a default-constructed <see cref="BencodeSerializationException" /> carries no byte offset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefault_ForBencodeSerializationException_ShouldLeaveBytesOffsetNull()
    {
        var ex = new BencodeSerializationException();

        Assert.IsNull(ex.BytesOffset);
        Assert.IsNull(ex.InnerException);
    }

    /// <summary>
    /// Verifies that the message constructor of <see cref="BencodeSerializationException" /> preserves the message
    /// and leaves the byte offset unset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenMessageProvided_ForBencodeSerializationException_ShouldPreserveMessage()
    {
        var ex = new BencodeSerializationException("binding failed");

        Assert.AreEqual("binding failed", ex.Message);
        Assert.IsNull(ex.BytesOffset);
    }

    /// <summary>
    /// Verifies that the inner-exception constructor of <see cref="BencodeSerializationException" /> preserves the
    /// chained exception.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenInnerExceptionProvided_ForBencodeSerializationException_ShouldPreserveChain()
    {
        var inner = new OverflowException("inner");
        var ex = new BencodeSerializationException("outer", inner);

        Assert.AreEqual("outer", ex.Message);
        Assert.AreSame(inner, ex.InnerException);
    }

    /// <summary>
    /// Verifies that the offset constructor of <see cref="BencodeSerializationException" /> records the byte offset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBytesOffsetProvided_ForBencodeSerializationException_ShouldRecordBytesOffset()
    {
        var ex = new BencodeSerializationException("binding failed", 23);

        Assert.AreEqual("binding failed", ex.Message);
        Assert.AreEqual(23, ex.BytesOffset);
    }
}
