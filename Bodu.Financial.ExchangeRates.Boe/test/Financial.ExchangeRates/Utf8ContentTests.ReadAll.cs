// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8ContentTests.ReadAll.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public sealed partial class Utf8ContentTests
{
    /// <summary>
    /// Verifies that reading a stream returns its bytes from its position to its end.
    /// </summary>
    [TestMethod]
    public void ReadAll_WhenStreamIsPartlyRead_ShouldReturnTheBytesFromItsPosition()
    {
        using MemoryStream stream = new([1, 2, 3, 4, 5], writable: false);
        stream.Position = 2;

        byte[] bytes = Utf8Content.ReadAll(stream);

        CollectionAssert.AreEqual(new byte[] { 3, 4, 5 }, bytes);
    }

    /// <summary>
    /// Verifies that reading a stream disposes it, as a <see cref="StreamReader" /> over it would.
    /// </summary>
    [TestMethod]
    public void ReadAll_WhenStreamIsRead_ShouldDisposeIt()
    {
        using MemoryStream stream = new([1, 2, 3], writable: false);

        _ = Utf8Content.ReadAll(stream);

        Assert.IsFalse(stream.CanRead);
    }

    /// <summary>
    /// Verifies that reading an empty stream returns no bytes.
    /// </summary>
    [TestMethod]
    public void ReadAll_WhenStreamIsEmpty_ShouldReturnNoBytes()
    {
        using MemoryStream stream = new([], writable: false);

        byte[] bytes = Utf8Content.ReadAll(stream);

        Assert.IsEmpty(bytes);
    }
}
