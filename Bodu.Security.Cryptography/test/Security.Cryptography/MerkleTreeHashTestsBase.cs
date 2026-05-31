// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeHashTestsBase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Abstract base class containing every test scenario that applies equally to both
/// <see cref="MerkleTreeHash" /> and <see cref="ParallelMerkleTreeHash" />.
/// </summary>
/// <typeparam name="THasher">
/// The concrete hasher type under test. Must be <see cref="IDisposable" /> because all hashers
/// own a pool of <see cref="HashAlgorithm" /> instances that must be released.
/// </typeparam>
/// <remarks>
/// <para>
/// MSTest discovers <c>[TestMethod]</c> members on any class derived from a <c>[TestClass]</c>
/// class — even when the base class is not itself decorated. Each derived test class therefore
/// inherits this full suite of tests and only needs to supply thin factory thunks.
/// </para>
/// <para>
/// Tests that depend on implementation-specific behaviour (for example,
/// <see cref="ObjectDisposedException" /> on post-dispose access, which only the parallel
/// implementation throws) live in partial files on the derived classes, not here.
/// </para>
/// </remarks>
public abstract partial class MerkleTreeHashTestsBase<THasher>
    where THasher : IDisposable
{
    /// <summary>Gets or sets the MSTest-supplied context. Required for <c>DataRow</c> logging.</summary>
    public TestContext TestContext { get; set; } = null!;

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Abstract adapters — each derived class wires these into its concrete type and overloads.
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Constructs a fresh hasher. Passing <see langword="null" /> for <paramref name="factory" />
    /// must route the call to the constructor unchanged so argument validation surfaces.
    /// </summary>
    /// <param name="factory">The hash algorithm factory, or <see langword="null" /> to exercise null-factory validation.</param>
    /// <param name="blockSize">The block size, or <see langword="null" /> to use the implementation default.</param>
    /// <param name="fanOut">The fan-out, or <see langword="null" /> to use the implementation default.</param>
    protected abstract THasher Construct(
        Func<HashAlgorithm>? factory,
        int? blockSize = null,
        int? fanOut = null);
}
