// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCipherTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Shared base for the wide-block tweakable Serpent block-cipher-tier tests
/// (<see cref="Serpent256CipherTests" />, <see cref="Serpent512CipherTests" />,
/// <see cref="Serpent1024CipherTests" />). Inherits the full
/// <see cref="BlockCipherTests{TTest, TCipher, TVariant}" /> contract surface and hoists the
/// <c>CreateBlockCipher</c> / <c>CreateBlockCipherForAnswer</c> wiring that differs only by which
/// <see cref="Serpent256Cipher" /> / <see cref="Serpent512Cipher" /> / <see cref="Serpent1024Cipher" />
/// constructor a row hands to.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve specification data for
/// <see cref="DynamicDataAttribute" /> sources via the standard <c>new TTest()</c> dispatch idiom.</typeparam>
/// <typeparam name="TCipher">The concrete <see cref="SerpentBlockCipherBase" />-derived engine under test.</typeparam>
public abstract partial class SerpentCipherTests<TTest, TCipher>
    : BlockCipherTests<TTest, TCipher, TweakableBlockCipherVariant>
    where TTest : SerpentCipherTests<TTest, TCipher>, new()
    where TCipher : SerpentBlockCipherBase
{
    /// <summary>
    /// Constructs a fresh <typeparamref name="TCipher" /> seeded with the supplied <paramref name="key" /> and
    /// <paramref name="tweak" />. Each wide-block Serpent variant binds this factory to its own size-specific
    /// constructor.
    /// </summary>
    /// <param name="key">The cipher key bytes.</param>
    /// <param name="tweak">The tweak bytes.</param>
    /// <returns>A new <typeparamref name="TCipher" /> instance.</returns>
    protected abstract TCipher CreateCipher(byte[] key, byte[] tweak);

    /// <inheritdoc />
    protected sealed override TCipher CreateBlockCipher(TweakableBlockCipherVariant variant)
    {
        BlockCipherSpecification spec = GetSpecification(variant);
        return CreateCipher(spec.TestKey, spec.TestTweak);
    }

    /// <inheritdoc />
    protected sealed override IBlockCipher CreateBlockCipherForAnswer(BlockCipherKnownAnswer answer) =>
        CreateCipher(answer.Key!, answer.Tweak!);
}
