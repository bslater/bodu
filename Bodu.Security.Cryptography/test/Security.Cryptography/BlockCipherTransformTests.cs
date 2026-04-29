// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Base class for testing <see cref="BlockCipherTransform" /> implementations. Extends
/// <see cref="CryptoTransformTests{TCryptoTransform}" /> with tests specific to block cipher transforms,
/// including null-argument validation, property invariants, and data-driven known-answer coverage at the
/// <see cref="System.Security.Cryptography.ICryptoTransform" /> layer.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve per-row known-answer vectors for
/// <see cref="DynamicDataAttribute" /> sources via the standard <c>new TTest()</c> dispatch idiom.</typeparam>
/// <typeparam name="TCryptoTransform">The concrete block cipher transform type under test.</typeparam>
public abstract partial class BlockCipherTransformTests<TTest, TCryptoTransform>
    : CryptoTransformTests<TCryptoTransform>
    where TTest : BlockCipherTransformTests<TTest, TCryptoTransform>, new()
    where TCryptoTransform : BlockCipherTransform
{
}
