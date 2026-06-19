// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoTransformTests{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Base class for testing types implementing <see cref="System.Security.Cryptography.ICryptoTransform" />.
/// </summary>
/// <typeparam name="TCryptoTransform">The crypto transform type under test.</typeparam>
public abstract partial class CryptoTransformTests<TCryptoTransform>
    where TCryptoTransform : ICryptoTransform
{
    /// <summary>
    /// Creates a new instance of the cryptographic transform under test.
    /// </summary>
    /// <returns>
    /// A newly constructed and fully initialised instance of <typeparamref name="TCryptoTransform" />, ready for use in test scenarios.
    /// </returns>
    protected abstract TCryptoTransform CreateAlgorithm();

    /// <summary>
    /// Gets a value indicating whether the implementation under test throws
    /// <see cref="ArgumentOutOfRangeException" /> (rather than the base <see cref="ArgumentException" />) when
    /// <c>inputCount</c> is negative. Domain transforms that delegate to
    /// <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" /> throw the more-specific
    /// <see cref="ArgumentOutOfRangeException" />; BCL <see cref="System.Security.Cryptography.HashAlgorithm" />
    /// implementations throw the base <see cref="ArgumentException" />.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the implementation throws <see cref="ArgumentOutOfRangeException" /> for a
    /// negative count; <see langword="false" /> when it throws <see cref="ArgumentException" />.
    /// </returns>
    protected virtual bool NegativeInputCountThrowsOutOfRange => true;
}
