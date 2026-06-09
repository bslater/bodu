// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base85Tests.Coverage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public partial class Base85Tests
{
    /// <summary>
    /// Verifies that the offset/count decode overload decodes the addressed subrange, round-tripping the input.
    /// </summary>
    [TestMethod]
    public void Decode_WithOffsetAndCount_ShouldRoundTrip()
    {
        byte[] data = { 1, 2, 3, 4 };
        char[] encoded = Base85.Encode(data).ToCharArray();

        var decoded = Base85.Decode(encoded, 0, encoded.Length);

        CollectionAssert.AreEqual(data, decoded);
    }

    /// <summary>
    /// Verifies that <see cref="Base85.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, Base85Variant,
    /// BaseFormatStyles)" /> returns <see langword="false" /> when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenDestinationTooSmall_ShouldReturnFalse()
    {
        byte[] data = { 1, 2, 3, 4 };
        char[] encoded = Base85.Encode(data).ToCharArray();

        var decoded = Base85.TryDecode(encoded, Span<byte>.Empty, out _);

        Assert.IsFalse(decoded);
    }
}
