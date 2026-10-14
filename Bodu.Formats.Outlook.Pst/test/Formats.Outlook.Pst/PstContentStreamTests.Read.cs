// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstContentStreamTests.Read.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;
using Bodu.Test.IO;

namespace Bodu.Formats.Outlook.Pst;

public partial class PstContentStreamTests
{
    /// <summary>
    /// Verifies that both read overloads return the wrapped stream's bytes.
    /// </summary>
    [TestMethod]
    public void Read_WhenInnerStreamReads_ShouldReturnItsBytes()
    {
        using PstContentStream stream = Over([1, 2, 3, 4]);
        var array = new byte[2];
        Span<byte> span = stackalloc byte[2];

        int first = stream.Read(array, 0, array.Length);
        int second = stream.Read(span);

        Assert.AreEqual(2, first);
        Assert.AreEqual(2, second);
        CollectionAssert.AreEqual(new byte[] { 1, 2 }, array);
        CollectionAssert.AreEqual(new byte[] { 3, 4 }, span.ToArray());
    }

    /// <summary>
    /// Verifies that a container fault raised by the array read overload surfaces as
    /// <see cref="OutlookPstFormatException" />, keeping the container exception as the inner exception.
    /// </summary>
    [TestMethod]
    public void Read_WhenInnerStreamThrowsPstFileException_ShouldThrowOutlookPstFormatException()
    {
        var fault = new PstFileFormatException("corrupt block", PstFileError.InvalidBlock);
        using var stream = new PstContentStream(new ThrowOnReadStream(() => fault));

        var ex = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = stream.Read(new byte[4], 0, 4);
        });

        Assert.AreSame(fault, ex.InnerException);
    }

    /// <summary>
    /// Verifies that a container fault raised by the span read overload surfaces as
    /// <see cref="OutlookPstFormatException" />, keeping the container exception as the inner exception.
    /// </summary>
    [TestMethod]
    public void Read_WhenInnerStreamThrowsPstFileException_ForSpan_ShouldThrowOutlookPstFormatException()
    {
        var fault = new PstFileFormatException("corrupt block", PstFileError.InvalidBlock);
        using var stream = new PstContentStream(new ThrowOnReadStream(() => fault));

        var ex = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
        {
            _ = stream.Read(new byte[4].AsSpan());
        });

        Assert.AreSame(fault, ex.InnerException);
    }

    /// <summary>
    /// Verifies that an exception that is not a container fault, such as an I/O failure of the source, propagates
    /// unchanged.
    /// </summary>
    [TestMethod]
    public void Read_WhenInnerStreamThrowsIOException_ShouldPropagateItUnchanged()
    {
        var failure = new IOException("device lost");
        using var stream = new PstContentStream(new ThrowOnReadStream(() => failure));

        IOException ex = Assert.ThrowsExactly<IOException>(() =>
        {
            _ = stream.Read(new byte[4], 0, 4);
        });

        Assert.AreSame(failure, ex);
    }
}
