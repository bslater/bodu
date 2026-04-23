// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelegateHashAlgorithmFactory.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a delegate-based implementation of <see cref="IHashAlgorithmFactory{T}" /> for constructing hash algorithm instances.
/// </summary>
/// <typeparam name="T">The concrete <see cref="HashAlgorithm" /> type this factory produces.</typeparam>
/// <remarks>
/// This factory allows consumers to specify the construction logic for the hash algorithm via a delegate. It is particularly useful for
/// scenarios requiring configuration of keyed or parameterized algorithms where DI or explicit configuration is preferred.
/// <para>
/// Common use cases include configuring <see cref="KeyedHashAlgorithm" /> instances or variant-based hashes such as SipHash or CubeHash.
/// </para>
/// </remarks>
public sealed class DelegateHashAlgorithmFactory<T> :
    Bodu.Security.Cryptography.IHashAlgorithmFactory<T>
    where T : System.Security.Cryptography.HashAlgorithm
{
    private readonly Func<T> _builder;

    /// <summary>
    /// Initialises a new instance of the <see cref="DelegateHashAlgorithmFactory{T}" /> class using the specified construction delegate.
    /// </summary>
    /// <param name="builder">
    /// A delegate that returns a configured instance of <typeparamref name="T" />. This delegate is invoked each time
    /// <see cref="Create" /> is called.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder" /> is <see langword="null" />.</exception>
    public DelegateHashAlgorithmFactory(Func<T> _builder)
    {
        this._builder = _builder ?? throw new ArgumentNullException(nameof(_builder));
    }

    /// <inheritdoc />
    public T Create() => this._builder();
}
