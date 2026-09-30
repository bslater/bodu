// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305CoreTests.UpdatePadded.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.Security.Cryptography;

public sealed partial class Poly1305CoreTests
{
    /// <summary>
    /// Verifies that a padded update produces the tag of the same bytes followed by explicit zeros up to the next
    /// 16-byte boundary of everything absorbed, for every segment length from empty to three blocks after an unpadded
    /// update of every alignment, and that the core then carries on unpadded.
    /// </summary>
    [TestMethod]
    public void UpdatePadded_WhenSegmentLengthVaries_ShouldMatchUpdateOfZeroPaddedData()
    {
        var random = new Random(0x1305_0005);

        for (int prefixLength = 0; prefixLength < Poly1305Core.BlockBytes; prefixLength++)
        {
            for (int length = 0; length <= 3 * Poly1305Core.BlockBytes; length++)
            {
                byte[] key = NextBytes(random, Poly1305Core.KeyBytes);
                byte[] prefix = NextBytes(random, prefixLength);
                byte[] data = NextBytes(random, length);
                byte[] suffix = NextBytes(random, random.Next(20));

                Poly1305Core core = default;
                core.Initialize(key);
                core.Update(prefix);
                core.UpdatePadded(data);
                core.Update(suffix);

                byte[] actual = new byte[Poly1305Core.TagBytes];
                core.Finish(actual);

                int zeros = (Poly1305Core.BlockBytes - ((prefixLength + length) % Poly1305Core.BlockBytes)) % Poly1305Core.BlockBytes;
                byte[] padded = [.. prefix, .. data, .. new byte[zeros], .. suffix];

                CollectionAssert.AreEqual(Poly1305Reference.ComputeTag(key, padded), actual, $"prefix {prefixLength}, length {length}");
            }
        }
    }

    /// <summary>
    /// Verifies that the RFC 8439 AEAD framing - the associated data and the ciphertext each padded to 16 bytes, then
    /// their lengths - reproduces the tag of the Section 2.8.2 example under its published one-time key.
    /// </summary>
    [TestMethod]
    public void UpdatePadded_WhenGivenRfc8439AeadExample_ShouldProduceTag()
    {
        byte[] key = Convert.FromHexString("7BAC2B252DB447AF09B67A55A4E955840AE1D6731075D9EB2A9375783ED553FF");
        byte[] associatedData = Convert.FromHexString("50515253C0C1C2C3C4C5C6C7");
        byte[] ciphertext = Convert.FromHexString(
            "D31A8D34648E60DB7B86AFBC53EF7EC2A4ADED51296E08FEA9E2B5A736EE62D6" +
            "3DBEA45E8CA9671282FAFB69DA92728B1A71DE0A9E060B2905D6A5B67ECD3B36" +
            "92DDBD7F2D778B8C9803AEE328091B58FAB324E4FAD675945585808B4831D7BC" +
            "3FF4DEF08E4B7A9DE576D26586CEC64B6116");
        byte[] expected = Convert.FromHexString("1AE10B594F09E26A7E902ECBD0600691");

        Poly1305Core core = default;
        core.Initialize(key);
        core.UpdatePadded(associatedData);
        core.UpdatePadded(ciphertext);

        byte[] lengths = new byte[Poly1305Core.BlockBytes];
        BinaryPrimitives.WriteUInt64LittleEndian(lengths, (ulong)associatedData.Length);
        BinaryPrimitives.WriteUInt64LittleEndian(lengths.AsSpan(sizeof(ulong)), (ulong)ciphertext.Length);
        core.Update(lengths);

        byte[] tag = new byte[Poly1305Core.TagBytes];
        core.Finish(tag);

        CollectionAssert.AreEqual(expected, tag);
    }
}
