// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CcmModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    // No published standard test vectors apply to CcmModeTransform:
    //
    // NIST SP 800-38C CCM uses a specific flag/nonce/counter formatting function (Section 6.3)
    // to construct the CBC-MAC input. CcmModeTransform uses a simplified nonce-based
    // counter without the standard formatting, making it non-interoperable with NIST
    // SP 800-38C Appendix C test vectors.
    //
    // Real-cipher round-trip coverage is provided by the inherited
    // EncryptThenDecrypt_WithRealAesCipher_RandomKey_ShouldRoundTrip test defined in AeadBlockCipherModeTests.KnownAnswerTests.cs.
    public sealed partial class CcmModeTransformTests
    {
    }
}