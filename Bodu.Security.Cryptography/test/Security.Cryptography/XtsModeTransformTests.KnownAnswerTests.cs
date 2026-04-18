// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XtsModeTransformTests.KnownAnswerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    // No published standard test vectors apply to XtsModeTransform:
    //
    // IEEE Std 1619-2007 / NIST SP 800-38E XTS uses two independent cipher keys: one for
    // the data path and one for the tweak. XtsModeTransform uses a single cipher instance
    // for both operations. The two-key standard and single-key implementation are not
    // interoperable, so IEEE 1619 section 7 test vectors do not apply.
    //
    // Real-cipher round-trip coverage is provided by the inherited
    // Transform_WithRealAesCipher_RandomKey_ShouldRoundTrip test defined in BlockCipherModeTests.KnownAnswerTests.cs.
    public sealed partial class XtsModeTransformTests
    {
    }
}