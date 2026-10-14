// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstContentStreamTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

namespace Bodu.Formats.Outlook.Pst;

public partial class PstContentStreamTests
{
    /// <summary>
    /// Verifies that disposing the content stream disposes the wrapped stream, and that flushing it beforehand is
    /// harmless.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalled_ShouldDisposeTheWrappedStream()
    {
        var inner = new DisposalTrackingStream(new MemoryStream([1, 2, 3]));
        var stream = new PstContentStream(inner);

        stream.Flush();
        stream.Dispose();

        Assert.IsTrue(inner.SyncDisposeCalled);
    }
}
