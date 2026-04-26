// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XxHash3.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

/// <summary>
/// Computes a 64-bit or 128-bit non-cryptographic hash using the <c>xxHash3</c> algorithm by Yann Collet.
/// This class cannot be inherited.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="XxHash3" /> is a highly optimised algorithm suitable for both short and long inputs. It uses a
/// 192-byte internal secret to drive per-lane mixing across 64-byte stripes, and selects an appropriate code
/// path based on input length: a short path for 0–16 bytes, a medium path for 17–240 bytes, and a full
/// stripe-accumulation path for inputs larger than 240 bytes.
/// </para>
/// <para>
/// When a non-zero seed is supplied, the internal secret is derived from the default secret by XOR-ing the
/// 64-bit seed into each 8-byte secret word. This produces an independent hash family for the same input.
/// </para>
/// <para>
/// The constructor's <paramref name="hashSize" /> parameter selects between 64-bit (8-byte) and 128-bit
/// (16-byte) output. Both sizes share the same accumulation phase; only the output extraction differs.
/// </para>
/// <note type="important">
/// This algorithm is <b>not</b> cryptographically secure and must <b>not</b> be used for password hashing,
/// digital signatures, or any application requiring adversarial collision resistance.
/// </note>
/// </remarks>
public sealed class XxHash3
    : XxHash<XxHash3>
{
    // Number of 64-bit accumulators used during the stripe phase.
    private const int AccumulatorCount = 8;

    // Number of bytes per stripe consumed during the long-input path.
    private const int StripeLen = 64;

    // Number of stripes processed per secret-key cycle.
    private const int StripesPerBlock = (SecretLen - StripeLen) / 8;

    // Length of the default secret, in bytes.
    private const int SecretLen = 192;

    // Minimum input length for the long (stripe-accumulation) path.
    private const int MidSizeMax = 240;

    private static readonly int[] ValidHashSizes = { 64, 128 };

    // The default 192-byte secret defined by the xxHash3 specification.
    private static readonly byte[] DefaultSecret =
    {
        0xb8, 0xfe, 0x6c, 0x39, 0x23, 0xa4, 0x4b, 0xbe, 0x7c, 0x01, 0x81, 0x2c, 0xf7, 0x21, 0xad, 0x1c,
        0xde, 0xd4, 0x6d, 0xe9, 0x83, 0x90, 0x97, 0xdb, 0x72, 0x40, 0xa4, 0xa4, 0xb7, 0xb3, 0x67, 0x1f,
        0xcb, 0x79, 0xe6, 0x4e, 0xcc, 0xc0, 0xe5, 0x78, 0x82, 0x5a, 0xd0, 0x7d, 0xcc, 0xff, 0x72, 0x21,
        0xb8, 0x08, 0x46, 0x74, 0xf7, 0x43, 0x24, 0x8e, 0xe0, 0x35, 0x90, 0xe6, 0x81, 0x3a, 0x26, 0x4c,
        0x3c, 0x28, 0x52, 0xbb, 0x91, 0xc3, 0x00, 0xcb, 0x88, 0xd0, 0x65, 0x8b, 0x1b, 0x53, 0x2e, 0xa3,
        0x71, 0x64, 0x48, 0x97, 0xa2, 0x0d, 0xf9, 0x4e, 0x38, 0x19, 0xef, 0x46, 0xa9, 0xde, 0xac, 0xd8,
        0xa8, 0xfa, 0x76, 0x3f, 0xe3, 0x9c, 0x34, 0x3f, 0xf9, 0xdc, 0xbb, 0xc7, 0xc7, 0x0b, 0x4f, 0x1d,
        0x8a, 0x51, 0xe0, 0x4b, 0xcd, 0xb4, 0x59, 0x31, 0xc8, 0x9f, 0x7e, 0xc9, 0xd9, 0x78, 0x73, 0x64,
        0xea, 0xc5, 0xac, 0x83, 0x34, 0xd3, 0xeb, 0xc3, 0xc5, 0x81, 0xa0, 0xff, 0xfa, 0x13, 0x63, 0xeb,
        0x17, 0x0d, 0xdd, 0x51, 0xb7, 0xf0, 0xda, 0x49, 0xd3, 0x16, 0x55, 0x26, 0x29, 0xd4, 0x68, 0x9e,
        0x2b, 0x16, 0xbe, 0x58, 0x7d, 0x47, 0xa1, 0xfc, 0x8f, 0xf8, 0xb8, 0xd1, 0x7a, 0xd0, 0x31, 0xce,
        0x45, 0xcb, 0x3a, 0x8f, 0x95, 0x16, 0x04, 0x28, 0xaf, 0xd7, 0xfb, 0xca, 0xbb, 0x4b, 0x40, 0x7e,
    };

    private readonly byte[] _secret;

    /// <summary>
    /// Initializes a new instance of the <see cref="XxHash3" /> class with a 64-bit output and a seed of zero.
    /// </summary>
    public XxHash3()
        : this(64, 0uL)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XxHash3" /> class with the specified output size and a seed of zero.
    /// </summary>
    /// <param name="hashSize">The desired hash output size in bits. Must be 64 or 128.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the supported values (64 or 128).
    /// </exception>
    public XxHash3(int hashSize)
        : this(hashSize, 0uL)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XxHash3" /> class with the specified output size and seed.
    /// </summary>
    /// <param name="hashSize">The desired hash output size in bits. Must be 64 or 128.</param>
    /// <param name="seed">The 64-bit seed used to derive the internal secret.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="hashSize" /> is not one of the supported values (64 or 128).
    /// </exception>
    public XxHash3(int hashSize, ulong seed)
        : base(ValidateHashSize(hashSize) / 8)
    {
        this.Seed = seed;
        this._secret = DeriveSecret(seed);
    }

    /// <summary>
    /// Gets the 64-bit seed used to derive the internal secret.
    /// </summary>
    /// <returns>The seed value supplied at construction time, or zero if no seed was specified.</returns>
    public ulong Seed { get; }

    /// <summary>
    /// Computes the xxHash3 of the provided input span using the configured output size.
    /// </summary>
    /// <param name="source">The input bytes to hash.</param>
    /// <returns>A byte array containing the 8-byte (64-bit) or 16-byte (128-bit) hash value.</returns>
    protected override byte[] ComputeHashCore(ReadOnlySpan<byte> source)
    {
        if (this.HashLengthInBytes == 8)
            return Hash64(source, this._secret);

        return Hash128(source, this._secret);
    }

    // ---- 64-bit output paths ----

    private static byte[] Hash64(ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        ulong h64 = source.Length switch
        {
            0 => Hash64Len0(secret),
            <= 3 => Hash64Len1To3(source, secret),
            <= 8 => Hash64Len4To8(source, secret),
            <= 16 => Hash64Len9To16(source, secret),
            <= 128 => Hash64Len17To128(source, secret),
            <= MidSizeMax => Hash64Len129To240(source, secret),
            _ => Hash64Long(source, secret),
        };

        byte[] result = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(result, h64);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Hash64Len0(ReadOnlySpan<byte> secret)
    {
        ulong s0 = BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(56));
        ulong s1 = BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(64));
        return Avalanche(unchecked(s0 ^ s1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Hash64Len1To3(ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        byte c1 = source[0];
        byte c2 = source[source.Length >> 1];
        byte c3 = source[source.Length - 1];
        uint combined = ((uint)c1 << 16) | ((uint)c2 << 24) | c3 | ((uint)source.Length << 8);
        ulong lo = BinaryPrimitives.ReadUInt32LittleEndian(secret) ^ BinaryPrimitives.ReadUInt32LittleEndian(secret.Slice(4));
        ulong mixed = unchecked((ulong)combined ^ lo);
        return Avalanche(mixed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Hash64Len4To8(ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        ulong input1 = BinaryPrimitives.ReadUInt32LittleEndian(source);
        ulong input2 = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(source.Length - 4));
        ulong bitFlip = unchecked(
            (BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(8)) ^
             BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(16))));
        ulong keyed = unchecked((input2 + ((input1 ^ bitFlip) << 32)));
        return RrmXxH64(keyed, unchecked((ulong)source.Length));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Hash64Len9To16(ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        ulong bitFlipLo = BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(24)) ^
                          BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(32));
        ulong bitFlipHi = BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(40)) ^
                          BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(48));
        ulong input1 = BinaryPrimitives.ReadUInt64LittleEndian(source) ^ bitFlipLo;
        ulong input2 = BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(source.Length - 8)) ^ bitFlipHi;
        ulong acc = unchecked((ulong)source.Length + BinaryPrimitives.ReverseEndianness(input1) + input2 + Mul128Fold64(input1, input2));
        return Avalanche(acc);
    }

    private static ulong Hash64Len17To128(ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        ulong acc = unchecked((ulong)source.Length * Prime64_1);

        if (source.Length > 32)
        {
            if (source.Length > 64)
            {
                if (source.Length > 96)
                {
                    acc += MixStep(source.Slice(48), secret.Slice(96));
                    acc += MixStep(source.Slice(source.Length - 64), secret.Slice(112));
                }
                acc += MixStep(source.Slice(32), secret.Slice(64));
                acc += MixStep(source.Slice(source.Length - 48), secret.Slice(80));
            }
            acc += MixStep(source.Slice(16), secret.Slice(32));
            acc += MixStep(source.Slice(source.Length - 32), secret.Slice(48));
        }

        acc += MixStep(source, secret);
        acc += MixStep(source.Slice(source.Length - 16), secret.Slice(16));

        return Avalanche(acc);
    }

    private static ulong Hash64Len129To240(ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        ulong acc = unchecked((ulong)source.Length * Prime64_1);

        int nbRounds = source.Length / 16;
        for (int i = 0; i < 8; i++)
            acc += MixStep(source.Slice(i * 16), secret.Slice(i * 16));

        acc = Avalanche(acc);

        for (int i = 8; i < nbRounds; i++)
            acc += MixStep(source.Slice(i * 16), secret.Slice((i - 8) * 16 + 3));

        acc += MixStep(source.Slice(source.Length - 16), secret.Slice(SecretLen - 17));

        return Avalanche(acc);
    }

    private static ulong Hash64Long(ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        ulong[] acc = InitAccumulators();
        ConsumeStripes(acc, source, secret);
        return MergeLongAcc(acc, secret, (ulong)source.Length);
    }

    // ---- 128-bit output paths ----

    private static byte[] Hash128(ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        (ulong lo, ulong hi) = source.Length switch
        {
            0 => Hash128Len0(secret),
            <= 3 => Hash128Len1To3(source, secret),
            <= 8 => Hash128Len4To8(source, secret),
            <= 16 => Hash128Len9To16(source, secret),
            <= 128 => Hash128Len17To128(source, secret),
            <= MidSizeMax => Hash128Len129To240(source, secret),
            _ => Hash128Long(source, secret),
        };

        byte[] result = new byte[16];
        BinaryPrimitives.WriteUInt64BigEndian(result.AsSpan(0, 8), lo);
        BinaryPrimitives.WriteUInt64BigEndian(result.AsSpan(8, 8), hi);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (ulong Lo, ulong Hi) Hash128Len0(ReadOnlySpan<byte> secret)
    {
        ulong lo = Avalanche(unchecked(
            BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(64)) ^
            BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(72))));
        ulong hi = Avalanche(unchecked(
            BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(80)) ^
            BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(88))));
        return (lo, hi);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (ulong Lo, ulong Hi) Hash128Len1To3(ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        byte c1 = source[0];
        byte c2 = source[source.Length >> 1];
        byte c3 = source[source.Length - 1];
        uint lo32 = ((uint)c1 << 16) | ((uint)c2 << 24) | c3 | ((uint)source.Length << 8);
        uint hi32 = BinaryPrimitives.ReverseEndianness(lo32);
        ulong lo = Avalanche(unchecked((ulong)lo32 ^ (BinaryPrimitives.ReadUInt32LittleEndian(secret) |
                                                       (ulong)BinaryPrimitives.ReadUInt32LittleEndian(secret.Slice(4)) << 32)));
        ulong hi = Avalanche(unchecked((ulong)hi32 ^ (BinaryPrimitives.ReadUInt32LittleEndian(secret.Slice(8)) |
                                                       (ulong)BinaryPrimitives.ReadUInt32LittleEndian(secret.Slice(12)) << 32)));
        return (lo, hi);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (ulong Lo, ulong Hi) Hash128Len4To8(ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        ulong input1 = BinaryPrimitives.ReadUInt32LittleEndian(source);
        ulong input2 = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(source.Length - 4));
        ulong bitFlipLo = BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(16)) ^
                          BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(24));
        ulong bitFlipHi = BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(32)) ^
                          BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(40));
        ulong lo = RrmXxH64(unchecked(input2 + ((input1 ^ bitFlipLo) << 32)), (ulong)source.Length);
        ulong hi = Avalanche(unchecked(input1 + ((input2 ^ bitFlipHi) << 32)));
        return (lo, hi);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (ulong Lo, ulong Hi) Hash128Len9To16(ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        ulong bitFlipLo = BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(32)) ^
                          BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(40));
        ulong bitFlipHi = BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(48)) ^
                          BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(56));
        ulong input1 = BinaryPrimitives.ReadUInt64LittleEndian(source) ^ bitFlipLo;
        ulong input2 = BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(source.Length - 8)) ^ bitFlipHi;
        ulong acc = unchecked((ulong)source.Length + BinaryPrimitives.ReverseEndianness(input1) + input2 + Mul128Fold64(input1, input2));
        ulong lo = Avalanche(acc);
        ulong hi = Avalanche(unchecked(acc * Prime64_1 + (ulong)source.Length * Prime64_4 + ((ulong)source.Length ^ bitFlipLo)));
        return (lo, hi);
    }

    private static (ulong Lo, ulong Hi) Hash128Len17To128(ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        ulong accLo = unchecked((ulong)source.Length * Prime64_1);
        ulong accHi = 0uL;

        if (source.Length > 32)
        {
            if (source.Length > 64)
            {
                if (source.Length > 96)
                {
                    accLo += MixStep(source.Slice(48), secret.Slice(96));
                    accHi += MixStep(source.Slice(source.Length - 64), secret.Slice(112));
                    accLo += MixStep(source.Slice(32), secret.Slice(64));
                    accHi += MixStep(source.Slice(source.Length - 48), secret.Slice(80));
                }
                else
                {
                    accLo += MixStep(source.Slice(32), secret.Slice(64));
                    accHi += MixStep(source.Slice(source.Length - 48), secret.Slice(80));
                }
            }
            accLo += MixStep(source.Slice(16), secret.Slice(32));
            accHi += MixStep(source.Slice(source.Length - 32), secret.Slice(48));
        }

        accLo += MixStep(source, secret);
        accHi += MixStep(source.Slice(source.Length - 16), secret.Slice(16));

        ulong lo = Avalanche(unchecked(accLo + accHi));
        ulong hi = Avalanche(unchecked(accLo * Prime64_4 + accHi * Prime64_2 + ((ulong)source.Length * Prime64_2)));
        return (lo, unchecked(~hi));
    }

    private static (ulong Lo, ulong Hi) Hash128Len129To240(ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        ulong accLo = unchecked((ulong)source.Length * Prime64_1);
        ulong accHi = 0uL;
        int nbRounds = source.Length / 32;

        for (int i = 0; i < 4; i++)
        {
            accLo += MixStep(source.Slice(i * 32), secret.Slice(i * 32));
            accHi += MixStep(source.Slice(i * 32 + 16), secret.Slice(i * 32 + 16));
        }

        accLo = Avalanche(accLo);
        accHi = Avalanche(accHi);

        for (int i = 4; i < nbRounds; i++)
        {
            accLo += MixStep(source.Slice(i * 32), secret.Slice((i - 4) * 32 + 3));
            accHi += MixStep(source.Slice(i * 32 + 16), secret.Slice((i - 4) * 32 + 19));
        }

        accLo += MixStep(source.Slice(source.Length - 16), secret.Slice(SecretLen - 17));
        accHi += MixStep(source.Slice(source.Length - 32), secret.Slice(SecretLen - 33));

        ulong lo = Avalanche(unchecked(accLo + accHi));
        ulong hi = Avalanche(unchecked(accLo * Prime64_4 + accHi * Prime64_2 + ((ulong)source.Length * Prime64_2)));
        return (lo, unchecked(~hi));
    }

    private static (ulong Lo, ulong Hi) Hash128Long(ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        ulong[] acc = InitAccumulators();
        ConsumeStripes(acc, source, secret);

        ulong lo = MergeLongAcc(acc, secret, (ulong)source.Length);
        ulong hi = MergeLongAccHi(acc, secret, (ulong)source.Length);
        return (lo, hi);
    }

    // ---- Shared accumulation primitives ----

    private static ulong[] InitAccumulators() => new ulong[]
    {
        Prime32_3, Prime64_1, Prime64_2, Prime64_3,
        Prime64_4, Prime32_2, Prime64_5, Prime32_1,
    };

    private static void ConsumeStripes(ulong[] acc, ReadOnlySpan<byte> source, ReadOnlySpan<byte> secret)
    {
        // Secret offset for the final stripe — intentionally not aligned to the 8-byte stride used
        // by regular stripes so that the last accumulation uses a distinct secret region.
        const int LastAccSecretStart = 7;

        int len = source.Length;
        int pos = 0;
        int blockLen = StripeLen * StripesPerBlock;

        while (pos + blockLen <= len)
        {
            for (int stripe = 0; stripe < StripesPerBlock; stripe++)
                Accumulate512(acc, source.Slice(pos + stripe * StripeLen), secret.Slice(stripe * 8));
            ScrambleAcc(acc, secret.Slice(SecretLen - StripeLen));
            pos += blockLen;
        }

        // Use (len - pos - 1) / StripeLen so that at least one byte is always reserved for the
        // unconditional final stripe below, preventing the last full stripe from being processed twice.
        int remainingStripes = (len - pos - 1) / StripeLen;
        for (int stripe = 0; stripe < remainingStripes; stripe++)
            Accumulate512(acc, source.Slice(pos + stripe * StripeLen), secret.Slice(stripe * 8));

        // Final stripe always covers the last StripeLen bytes, potentially overlapping the last
        // regular stripe for non-aligned inputs. Secret offset matches the reference formula.
        Accumulate512(acc, source.Slice(len - StripeLen), secret.Slice(SecretLen - StripeLen - LastAccSecretStart));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Accumulate512(ulong[] acc, ReadOnlySpan<byte> stripe, ReadOnlySpan<byte> secret)
    {
        for (int i = 0; i < AccumulatorCount; i++)
        {
            ulong dataVal = BinaryPrimitives.ReadUInt64LittleEndian(stripe.Slice(i * 8, 8));
            ulong dataKey = dataVal ^ BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(i * 8, 8));
            acc[i ^ 1] = unchecked(acc[i ^ 1] + dataVal);
            acc[i] = unchecked(acc[i] + (uint)dataKey * (dataKey >> 32));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ScrambleAcc(ulong[] acc, ReadOnlySpan<byte> secret)
    {
        for (int i = 0; i < AccumulatorCount; i++)
        {
            ulong key = BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(i * 8, 8));
            acc[i] ^= acc[i] >> 47;
            acc[i] ^= key;
            acc[i] = unchecked(acc[i] * Prime32_1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MergeLongAcc(ulong[] acc, ReadOnlySpan<byte> secret, ulong length)
    {
        const int SecretMergeStart = 11;
        ulong result = unchecked(length * Prime64_1);
        for (int i = 0; i < 4; i++)
        {
            ulong a1 = acc[2 * i]     ^ BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(SecretMergeStart + 16 * i));
            ulong a2 = acc[2 * i + 1] ^ BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(SecretMergeStart + 16 * i + 8));
            result = unchecked(result + Mul128Fold64(a1, a2));
        }

        return Avalanche(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MergeLongAccHi(ulong[] acc, ReadOnlySpan<byte> secret, ulong length)
    {
        const int SecretHiStart = SecretLen - 64;
        ulong result = unchecked(~(length * Prime64_2));
        for (int i = 0; i < 4; i++)
        {
            ulong a1 = acc[2 * i]     ^ BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(SecretHiStart + 16 * i));
            ulong a2 = acc[2 * i + 1] ^ BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(SecretHiStart + 16 * i + 8));
            result = unchecked(result + Mul128Fold64(a1, a2));
        }

        return Avalanche(result);
    }

    // ---- Utility helpers ----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MixStep(ReadOnlySpan<byte> data, ReadOnlySpan<byte> secret)
    {
        ulong dataLo = BinaryPrimitives.ReadUInt64LittleEndian(data);
        ulong dataHi = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(8));
        ulong keyLo = BinaryPrimitives.ReadUInt64LittleEndian(secret);
        ulong keyHi = BinaryPrimitives.ReadUInt64LittleEndian(secret.Slice(8));
        return Mul128Fold64(dataLo ^ keyLo, dataHi ^ keyHi);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Mul128Fold64(ulong lo, ulong hi)
    {
        ulong lo128 = unchecked(lo * hi);
        ulong hi128 = Math.BigMul(lo, hi, out ulong _);
        return unchecked(lo128 ^ hi128);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Avalanche(ulong h64)
    {
        h64 ^= h64 >> 37;
        h64 = unchecked(h64 * 0x165667919E3779F9uL);
        h64 ^= h64 >> 32;
        return h64;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong RrmXxH64(ulong h64, ulong length)
    {
        h64 ^= RotateLeft64(h64, 49) ^ RotateLeft64(h64, 24);
        h64 = unchecked(h64 * 0x9FB21C651E98DF25uL);
        h64 ^= unchecked((h64 >> 35) + length);
        h64 = unchecked(h64 * 0x9FB21C651E98DF25uL);
        h64 ^= h64 >> 28;
        return h64;
    }

    private static byte[] DeriveSecret(ulong seed)
    {
        if (seed == 0)
            return (byte[])DefaultSecret.Clone();

        byte[] secret = new byte[SecretLen];
        for (int i = 0; i < SecretLen / 16; i++)
        {
            ulong lo = BinaryPrimitives.ReadUInt64LittleEndian(DefaultSecret.AsSpan(i * 16)) + seed;
            ulong hi = BinaryPrimitives.ReadUInt64LittleEndian(DefaultSecret.AsSpan(i * 16 + 8)) - seed;
            BinaryPrimitives.WriteUInt64LittleEndian(secret.AsSpan(i * 16), lo);
            BinaryPrimitives.WriteUInt64LittleEndian(secret.AsSpan(i * 16 + 8), hi);
        }

        return secret;
    }

    private static int ValidateHashSize(int hashSize)
    {
        if (Array.IndexOf(ValidHashSizes, hashSize) == -1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(hashSize),
                hashSize,
                $"Invalid hash size: {hashSize}. Valid sizes are: {string.Join(", ", ValidHashSizes)}.");
        }

        return hashSize;
    }
}
