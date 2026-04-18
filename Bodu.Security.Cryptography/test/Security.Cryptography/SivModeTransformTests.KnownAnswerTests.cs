// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    // No published standard test vectors apply to SivModeTransform:
    //
    // RFC 5297 SIV-AES derives its synthetic IV using S2V, a CMAC-based PRF over the
    // associated data and plaintext. SivModeTransform uses a simplified CTR with bits 31
    // and 63 cleared, without the S2V computation. RFC 5297 Appendix A vectors do not apply.
    //
    // Real-cipher round-trip coverage is provided by the inherited
    // Transform_WithRealAesCipher_RandomKey_ShouldRoundTrip test defined in BlockCipherModeTests.KnownAnswerTests.cs.
    public sealed partial class SivModeTransformTests
    {
    }
}