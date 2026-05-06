// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.ProcessAssociatedData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconAead128Tests
{
    // ── ProcessAssociatedData state-machine ───────────────────────────────────────────────────

    /// <summary>
    /// Verifies that calling <see cref="AsconAead128.ProcessAssociatedData" /> a second time throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ProcessAssociatedData_WhenCalledTwice_ShouldThrowInvalidOperationException()
    {
        using AsconAead128 sut = new AsconAead128(ValidKey, ValidNonce);
        sut.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="AsconAead128.ProcessAssociatedData" /> on a disposed
    /// instance throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void ProcessAssociatedData_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        AsconAead128 sut = new AsconAead128(ValidKey, ValidNonce);
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);
        });
    }
}
