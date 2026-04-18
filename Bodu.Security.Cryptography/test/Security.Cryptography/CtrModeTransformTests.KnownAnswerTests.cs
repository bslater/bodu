// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtrModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    // No published standard test vectors apply to CtrModeTransform:
    //
    // NIST SP 800-38A F.5.1 CTR uses big-endian (most-significant-byte-last) counter
    // increment. CtrModeTransform uses little-endian increment to guard against wrap
    // via GuardsAgainstKeystreamReuse. The two counter orderings are not interoperable
    // so published NIST CTR vectors do not apply.
    //
    // Real-cipher round-trip coverage is provided by the inherited
    // Transform_WithRealAesCipher_RandomKey_ShouldRoundTrip test defined in BlockCipherModeTests.KnownAnswerTests.cs.
    public sealed partial class CtrModeTransformTests
    {
    }
}