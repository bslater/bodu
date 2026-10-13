// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base58CheckTests.TryDecode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Verifies <see cref="Base58Check.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, Base58Variant, BaseFormatStyles)" />
/// for payloads that begin with a run of zero bytes.
/// </summary>
public sealed partial class Base58CheckTests
{
    /// <summary>
    /// Verifies that <see cref="Base58Check.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, Base58Variant, BaseFormatStyles)" />
    /// recovers a zero-filled payload, for every variant and every payload length to 64, into a destination of
    /// <see cref="Base58Check.GetMaxDecodedLength(int)" /> bytes.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenPayloadIsZeroFilled_ShouldDecodeIntoTheMaxDecodedLength()
    {
        foreach (Base58Variant variant in Enum.GetValues<Base58Variant>())
        {
            for (int payloadLength = 0; payloadLength <= 64; payloadLength++)
            {
                string encoded = Base58Check.Encode(new byte[payloadLength], variant);
                byte[] destination = new byte[Base58Check.GetMaxDecodedLength(encoded.Length)];

                bool decoded = Base58Check.TryDecode(encoded, destination, out int bytesWritten, variant);

                Assert.IsTrue(decoded, $"Result for {variant} with a {payloadLength}-byte payload.");
                Assert.AreEqual(payloadLength, bytesWritten, $"Bytes written for {variant} with a {payloadLength}-byte payload.");
            }
        }
    }

    /// <summary>
    /// Verifies that <see cref="Base58Check.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, Base58Variant, BaseFormatStyles)" />
    /// recovers a zero-filled payload of every length to 128 bytes into a destination of exactly the payload's length,
    /// which its internal decode buffer must not undersize.
    /// </summary>
    [TestMethod]
    public void TryDecode_WhenPayloadIsALongRunOfZeroBytes_ShouldRecoverIt()
    {
        for (int payloadLength = 0; payloadLength <= 128; payloadLength++)
        {
            string encoded = Base58Check.Encode(new byte[payloadLength]);
            byte[] destination = new byte[payloadLength];

            bool decoded = Base58Check.TryDecode(encoded, destination, out int bytesWritten);

            Assert.IsTrue(decoded, $"Result with a {payloadLength}-byte payload.");
            Assert.AreEqual(payloadLength, bytesWritten, $"Bytes written with a {payloadLength}-byte payload.");
        }
    }
}
