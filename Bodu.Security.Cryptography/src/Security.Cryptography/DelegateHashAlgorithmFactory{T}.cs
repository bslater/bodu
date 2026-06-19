// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelegateHashAlgorithmFactory{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a delegate-based implementation of <see cref="IHashAlgorithmFactory{T}" /> for constructing hash algorithm
/// instances.
/// </summary>
/// <typeparam name="T">The concrete <see cref="HashAlgorithm" /> type this factory produces.</typeparam>
/// <remarks>
/// <para>
/// This factory allows consumers to specify the construction logic for the hash algorithm via a delegate. It is
/// particularly useful for scenarios requiring configuration of keyed or parameterized algorithms where DI or explicit
/// configuration is preferred.
/// </para>
/// <para>
/// Common use cases include configuring <see cref="KeyedHashAlgorithm" /> instances or variant-based hashes such as
/// SipHash or CubeHash.
/// </para>
/// <para>
/// <strong>Construction.</strong> Most callers use <see cref="HashAlgorithmFactory.From{T}(System.Func{T})" /> to wrap
/// a delegate rather than calling this constructor directly — the static helper makes the call site read more naturally
/// and avoids the trailing generic-type repetition. Direct construction is appropriate when the type needs to be
/// assigned to a field that has the concrete <see cref="DelegateHashAlgorithmFactory{T}" /> type rather than the
/// <see cref="IHashAlgorithmFactory{T}" /> interface.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Wrap a delegate via the static helper — the call site reads more naturally than the
/// // constructor and avoids the trailing generic-type repetition.
/// IHashAlgorithmFactory<SipHash64> factory = HashAlgorithmFactory.From(() =>
///     new SipHash64 { Key = sharedKey });
///
/// // Resolve and use — the factory hands out a freshly-keyed instance each time.
/// using SipHash64 hash = factory.Create();
/// byte[] digest = hash.ComputeHash("hello"u8.ToArray());
///
/// // Or construct directly when the field type needs to be the concrete factory.
/// var concreteFactory = new DelegateHashAlgorithmFactory<Tiger>(() => new Tiger
/// {
///     HashingVariant = TigerHashingVariant.Tiger2,
/// });
///]]>
/// </example>
/// <seealso cref="IHashAlgorithmFactory{T}"/> <seealso cref="HashAlgorithmFactory"/>
/// <seealso cref="HashAlgorithmHelper"/>
public sealed class DelegateHashAlgorithmFactory<T> :
    Bodu.Security.Cryptography.IHashAlgorithmFactory<T>
    where T : System.Security.Cryptography.HashAlgorithm
{
    /// <summary>The delegate invoked to construct a configured instance of <typeparamref name="T" /> on each <see cref="Create" /> call.</summary>
    private readonly Func<T> _builder;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelegateHashAlgorithmFactory{T}" /> class using the specified
    /// construction delegate.
    /// </summary>
    /// <param name="builder">
    /// A delegate that returns a configured instance of <typeparamref name="T" />. This delegate is invoked each time
    /// <see cref="Create" /> is called.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    public DelegateHashAlgorithmFactory(Func<T> builder)
    {
        _builder = builder ?? throw new ArgumentNullException(nameof(builder));
    }

    /// <inheritdoc />
    public T Create() => _builder();
}
