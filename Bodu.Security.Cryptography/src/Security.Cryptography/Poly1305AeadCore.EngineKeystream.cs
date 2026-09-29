// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305AeadCore.EngineKeystream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

internal static partial class Poly1305AeadCore
{
    /// <summary>
    /// Adapts an <see cref="IStreamCipher" /> engine to the <see cref="IKeystreamSource" /> the framings draw on.
    /// </summary>
    /// <remarks>
    /// Whole blocks go to an <see cref="IBulkStreamCipher" /> engine in one call, and to any other engine one keystream
    /// block at a time. The engine keeps its own exhaustion guard either way.
    /// </remarks>
    private readonly struct EngineKeystream
        : IKeystreamSource
    {
        /// <summary>The engine the keystream is drawn from.</summary>
        private readonly IStreamCipher _engine;

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineKeystream" /> struct over the specified engine.
        /// </summary>
        /// <param name="engine">The engine, positioned where the keystream starts.</param>
        internal EngineKeystream(IStreamCipher engine) =>
            _engine = engine;

        /// <inheritdoc />
        /// <remarks>
        /// Always <see cref="ChaCha20Core.KernelKind.Scalar" />: whatever kernels the engine has are not known here, so
        /// the framings draw from it as the keystream comes, and it supplies only the blocks the message needs.
        /// </remarks>
        public ChaCha20Core.KernelKind Kernel => ChaCha20Core.KernelKind.Scalar;

        /// <inheritdoc />
        public void NextBlock(Span<byte> destination) =>
            _engine.NextKeystreamBlock(destination);

        /// <inheritdoc />
        public void XorBlocks(ReadOnlySpan<byte> input, Span<byte> output)
        {
            if (_engine is IBulkStreamCipher bulk)
            {
                bulk.XorKeystreamBlocks(input, output);
                return;
            }

            Span<byte> keystream = stackalloc byte[KeystreamBlockBytes];

            try
            {
                for (int offset = 0; offset < input.Length; offset += KeystreamBlockBytes)
                {
                    _engine.NextKeystreamBlock(keystream);
                    CryptographyHelper.Xor(input.Slice(offset, KeystreamBlockBytes), keystream, output.Slice(offset, KeystreamBlockBytes));
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keystream);
            }
        }
    }
}
