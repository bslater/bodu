// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    // No published standard test vectors apply to OcbModeTransform:
    //
    // RFC 7253 OCB3 derives its initial offset via a K_top stretching step applied to the
    // nonce. OcbModeTransform seeds the offset directly from cipher.Encrypt(nonce),
    // bypassing the stretch. The simplified offset sequence is not interoperable with
    // RFC 7253 Appendix A test vectors.
    //
    // Real-cipher round-trip coverage is provided by the inherited
    // Transform_WithRealAesCipher_RandomKey_ShouldRoundTrip test defined in BlockCipherModeTests.KnownAnswerTests.cs.
    public sealed partial class OcbModeTransformTests
    {
    }
}