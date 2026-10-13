// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58Tests.TryDecode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Verifies <see cref="Base58.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, Base58Variant, BaseFormatStyles)" />
/// into a destination sized by <see cref="Base58.GetMaxDecodedLength(int)" />.
/// </summary>
public sealed partial class Base58Tests
{
    /// <summary>
    /// Verifies that <see cref="Base58.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, Base58Variant, BaseFormatStyles)" />
    /// decodes input made only of the alphabet's first character, for every variant and at every length to 64, into a
    /// destination of <see cref="Base58.GetMaxDecodedLength(int)" /> bytes.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenEveryCharacterIsALeadingZero_ShouldDecodeIntoTheMaxDecodedLength()
    {
        foreach (Base58Variant variant in Enum.GetValues<Base58Variant>())
        {
            for (int length = 1; length <= 64; length++)
            {
                byte[] destination = new byte[Base58.GetMaxDecodedLength(length)];

                bool decoded = Base58.TryDecode(LeadingZeroCharacters(variant, length), destination, out int bytesWritten, variant);

                Assert.IsTrue(decoded, $"Result for {variant} at length {length}.");
                Assert.AreEqual(length, bytesWritten, $"Bytes written for {variant} at length {length}.");
            }
        }
    }
}
