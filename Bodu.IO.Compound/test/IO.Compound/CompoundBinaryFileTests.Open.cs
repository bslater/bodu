// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundBinaryFileTests.Open.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundBinaryFileTests
{
    /// <summary>
    /// Verifies that opening a <see langword="null" /> stream throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CompoundBinaryFile.Open(null!);
        });

        Assert.AreEqual("stream", ex.ParamName);
    }

    /// <summary>
    /// Verifies that opening data whose leading bytes are not the compound-file signature throws
    /// <see cref="CompoundFileFormatException" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenSignatureIsInvalid_ShouldThrowCompoundFileFormatException()
    {
        using MemoryStream stream = new(new byte[600]);

        _ = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var file = CompoundBinaryFile.Open(stream);
        });
    }

    /// <summary>
    /// Verifies that opening data shorter than a header throws <see cref="CompoundFileFormatException" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenDataIsTooShort_ShouldThrowCompoundFileFormatException()
    {
        using MemoryStream stream = new(new byte[64]);

        _ = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var file = CompoundBinaryFile.Open(stream);
        });
    }

    /// <summary>
    /// Verifies that disposing with <c>leaveOpen</c> set keeps the source stream usable afterwards.
    /// </summary>
    [TestMethod]
    public void Open_WhenLeaveOpenIsTrue_ShouldNotDisposeSourceStream()
    {
        using MemoryStream source = CompoundFixtures.OpenStream(CompoundFixtures.SampleCompound);

        using (var file = CompoundBinaryFile.Open(source, leaveOpen: true))
        {
            Assert.IsTrue(file.Entries.Count > 0);
        }

        // The source stream must still be readable because leaveOpen was requested.
        Assert.IsTrue(source.CanRead);
    }
}
