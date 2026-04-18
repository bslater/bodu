// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmSivModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    // No published standard test vectors apply to GcmSivModeTransform:
    //
    // RFC 8452 GCM-SIV derives per-message keys from the nonce using the POLYVAL field
    // (GF(2^128) with a different polynomial) and a key derivation counter. GcmSivModeTransform
    // uses a simplified POLYVAL via GHASH isomorphism and a single-call key derivation.
    // RFC 8452 Appendix C vectors therefore do not apply.
    //
    // Real-cipher round-trip coverage is provided by the inherited
    // EncryptThenDecrypt_WithRealAesCipher_RandomKey_ShouldRoundTrip test defined in AeadBlockCipherModeTests.KnownAnswerTests.cs.
    public sealed partial class GcmSivModeTransformTests
    {
    }
}