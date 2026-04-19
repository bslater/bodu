// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Base class for testing <see cref="BlockCipherTransform" /> implementations. Extends
    /// <see cref="CryptoTransformTests{TCryptoTransform}" /> with tests specific to block cipher transforms,
    /// including null-argument validation and property invariants.
    /// </summary>
    /// <typeparam name="TCryptoTransform">The concrete block cipher transform type under test.</typeparam>
    public abstract partial class BlockCipherTransformTests<TCryptoTransform>
        : CryptoTransformTests<TCryptoTransform>
        where TCryptoTransform : BlockCipherTransform
    {
    }
}
