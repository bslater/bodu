// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base16Tests.Coverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public partial class Base16Tests
{
    /// <summary>
    /// Verifies that <see cref="Base16.TryDecodeGuid(ReadOnlySpan{char}, out System.Guid, BaseFormatStyles)" /> returns
    /// <see langword="false" /> when the source decodes to a length other than sixteen bytes.
    /// </summary>
    [TestMethod]
    public void TryDecodeGuid_WhenDecodedLengthIsNotSixteen_ShouldReturnFalse()
    {
        var decoded = Base16.TryDecodeGuid("00", out Guid value);

        Assert.IsFalse(decoded);
        Assert.AreEqual(Guid.Empty, value);
    }
}
