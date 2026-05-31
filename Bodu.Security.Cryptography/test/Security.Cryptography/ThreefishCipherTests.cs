// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishCipherTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Shared base for the tweakable Threefish block-cipher-tier tests
/// (<see cref="Threefish256CipherTests" />, <see cref="Threefish512CipherTests" />,
/// <see cref="Threefish1024CipherTests" />). Inherits the full
/// <see cref="BlockCipherTests{TTest, TCipher, TVariant}" /> contract surface and hoists the
/// <c>CreateBlockCipher</c> / <c>CreateBlockCipherForAnswer</c> wiring that differs only by which
/// <see cref="Threefish256Cipher" /> / <see cref="Threefish512Cipher" /> / <see cref="Threefish1024Cipher" />
/// constructor a row hands to.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve specification data for
/// <see cref="DynamicDataAttribute" /> sources via the standard <c>new TTest()</c> dispatch idiom.</typeparam>
/// <typeparam name="TCipher">The concrete <see cref="ThreefishBlockCipher" />-derived engine under test.</typeparam>
public abstract partial class ThreefishCipherTests<TTest, TCipher>
    : BlockCipherTests<TTest, TCipher, TweakableBlockCipherVariant>
    where TTest : ThreefishCipherTests<TTest, TCipher>, new()
    where TCipher : ThreefishBlockCipher
{
    /// <summary>
    /// Constructs a fresh <typeparamref name="TCipher" /> seeded with the supplied <paramref name="key" /> and
    /// <paramref name="tweak" />. Each Threefish variant binds this factory to its own size-specific constructor.
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
