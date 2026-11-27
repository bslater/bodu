// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Base62Tests.GetMaxDecodedLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

/// <summary>
/// Verifies <see cref="Base62.GetMaxDecodedLength(int)" />, the documented upper bound on the decoded byte count.
/// </summary>
public sealed partial class Base62Tests
{
    /// <summary>
    /// Verifies that <see cref="Base62.GetMaxDecodedLength(int)" /> is at least the decoded length of input made only
    /// of <c>0</c>, which decodes to one zero byte per character, at every length to 64.
    /// </summary>
    [TestMethod]
    public void GetMaxDecodedLength_WhenEveryCharacterIsALeadingZero_ShouldBoundTheDecodedLength()
    {
        for (int length = 1; length <= 64; length++)
        {
            byte[] decoded = Base62.Decode(new string('0', length));

            Assert.IsGreaterThanOrEqualTo(decoded.Length, Base62.GetMaxDecodedLength(length), $"Length {length}.");
        }
    }

    /// <summary>
    /// Verifies that <see cref="Base62.TryDecode(ReadOnlySpan{char}, Span{byte}, out int, BaseFormatStyles)" /> decodes
    /// the encoding of a payload that leads with zero bytes into a destination of
    /// <see cref="Base62.GetMaxDecodedLength(int)" /> bytes, at every payload length to 64.
    /// </summary>
    [TestMethod]
    public void GetMaxDecodedLength_WhenPayloadLeadsWithZeroBytes_ShouldSizeADestinationThatHoldsIt()
    {
        for (int length = 1; length <= 64; length++)
        {
            byte[] payload = new byte[length];
            payload[^1] = 1;
            string encoded = Base62.Encode(payload);

            bool success = Base62.TryDecode(encoded, new byte[Base62.GetMaxDecodedLength(encoded.Length)], out int bytesWritten);

            Assert.IsTrue(success, $"Length {length}.");
            Assert.AreEqual(length, bytesWritten, $"Length {length}.");
        }
    }
}
