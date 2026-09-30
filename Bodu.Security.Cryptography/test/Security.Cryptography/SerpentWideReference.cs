// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentWideReference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Numerics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides the wide-block Serpent variants as 1.1.0 computed them, kept as the oracle that the cipher is held to: each
/// round adds its whole round key, applies its S-box to every four-word group, applies the linear transform to every
/// group and rotates the words one position, and every fourth round injects the tweak.
/// </summary>
/// <remarks>
/// The S-boxes and the linear transform are <see cref="SerpentReference" />'s table-driven ones, so the oracle shares
/// no code with the cipher.
/// </remarks>
internal sealed class SerpentWideReference
{
    /// <summary>The golden-ratio constant of the prekey recurrence.</summary>
    private const uint Phi = 0x9E3779B9u;

    /// <summary>The number of 32-bit words in a block.</summary>
    private readonly int _words;

    /// <summary>The number of rounds.</summary>
    private readonly int _rounds;

    /// <summary>The round keys: <c>_rounds + 1</c> keys of <see cref="_words" /> words each.</summary>
    private readonly uint[] _roundKeys;

    /// <summary>The tweak schedule: the four tweak words and their XOR.</summary>
    private readonly uint[] _tweakSchedule;

    /// <summary>
    /// Initializes a new instance of the <see cref="SerpentWideReference" /> class, expanding a key and a tweak for the
    /// variant whose block is as long as the key: Serpent-256, 512 or 1024.
    /// </summary>
    /// <param name="key">The key: 32, 64 or 128 bytes, the length of a block.</param>
    /// <param name="tweak">The 16-byte tweak.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is not 32, 64 or 128 bytes, or <paramref name="tweak" /> is not 16 bytes.
    /// </exception>
    internal SerpentWideReference(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak)
        : this(key, tweak, key.Length switch
        {
            32 => 48,
            64 => 64,
            128 => 80,
            _ => throw new ArgumentException("The key must be 32, 64 or 128 bytes.", nameof(key)),
        })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerpentWideReference" /> class, expanding a key and a tweak for a
    /// block as long as the key and the specified number of rounds.
    /// </summary>
    /// <param name="key">The key: 32, 64 or 128 bytes, the length of a block.</param>
    /// <param name="tweak">The 16-byte tweak.</param>
    /// <param name="rounds">The number of rounds, a positive multiple of 4.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="key" /> is not 32, 64 or 128 bytes, or <paramref name="tweak" /> is not 16 bytes.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="rounds" /> is not a positive multiple of 4.</exception>
    internal SerpentWideReference(ReadOnlySpan<byte> key, ReadOnlySpan<byte> tweak, int rounds)
    {
        if (key.Length is not (32 or 64 or 128)) throw new ArgumentException("The key must be 32, 64 or 128 bytes.", nameof(key));
        if (tweak.Length != 16) throw new ArgumentException("The tweak must be 16 bytes.", nameof(tweak));
        if (rounds <= 0 || rounds % 4 != 0) throw new ArgumentOutOfRangeException(nameof(rounds));

        _words = key.Length / 4;
        _rounds = rounds;

        _tweakSchedule = new uint[5];
        for (int i = 0; i < 4; i++)
            _tweakSchedule[i] = BinaryPrimitives.ReadUInt32LittleEndian(tweak[(i * 4)..]);

        _tweakSchedule[4] = _tweakSchedule[0] ^ _tweakSchedule[1] ^ _tweakSchedule[2] ^ _tweakSchedule[3];

        uint[] prekeys = new uint[_words + ((_rounds + 1) * _words)];
        for (int i = 0; i < _words; i++)
            prekeys[i] = BinaryPrimitives.ReadUInt32LittleEndian(key[(i * 4)..]);

        for (int i = 0; i + _words < prekeys.Length; i++)
        {
            uint value = prekeys[i] ^ prekeys[i + _words - 5] ^ prekeys[i + _words - 3] ^ prekeys[i + _words - 1] ^ Phi ^ (uint)i;
            prekeys[i + _words] = BitOperations.RotateLeft(value, 11);
        }

        _roundKeys = new uint[(_rounds + 1) * _words];
        for (int r = 0; r <= _rounds; r++)
        {
            for (int g = 0; g < _words; g += 4)
            {
                int source = _words + (r * _words) + g;
                uint x0 = prekeys[source];
                uint x1 = prekeys[source + 1];
                uint x2 = prekeys[source + 2];
                uint x3 = prekeys[source + 3];

                SerpentReference.ApplySBox((3 - r) & 7, ref x0, ref x1, ref x2, ref x3);

                int destination = (r * _words) + g;
                _roundKeys[destination] = x0;
                _roundKeys[destination + 1] = x1;
                _roundKeys[destination + 2] = x2;
                _roundKeys[destination + 3] = x3;
            }
        }
    }

    /// <summary>
    /// Gets the number of 32-bit words in a block.
    /// </summary>
    internal int Words => _words;

    /// <summary>
    /// Gets the number of rounds.
    /// </summary>
    internal int Rounds => _rounds;

    /// <summary>
    /// Returns the round keys with the tweak folded in, the form <see cref="SerpentCore" />'s wide-block rounds take:
    /// the material injected after round <c>4j − 1</c> added to the last three words of round key <c>4j</c>.
    /// </summary>
    /// <returns>A new array holding the round keys.</returns>
    internal uint[] FoldedRoundKeys()
    {
        uint[] keys = (uint[])_roundKeys.Clone();
        for (int injection = 1; injection < _rounds / 4; injection++)
        {
            int start = 4 * injection * _words;
            keys[start + _words - 3] ^= _tweakSchedule[injection % 5];
            keys[start + _words - 2] ^= _tweakSchedule[(injection + 1) % 5];
            keys[start + _words - 1] ^= (uint)injection;
        }

        return keys;
    }

    /// <summary>
    /// Encrypts one block.
    /// </summary>
    /// <param name="input">The plaintext block.</param>
    /// <returns>The ciphertext block.</returns>
    internal byte[] Encrypt(ReadOnlySpan<byte> input)
    {
        uint[] state = ReadWords(input);
        int injection = 0;

        for (int r = 0; r < _rounds - 1; r++)
        {
            AddRoundKey(state, r);
            for (int g = 0; g < _words; g += 4)
                SerpentReference.ApplySBox(r & 7, ref state[g], ref state[g + 1], ref state[g + 2], ref state[g + 3]);

            for (int g = 0; g < _words; g += 4)
                SerpentReference.LinearTransform(ref state[g], ref state[g + 1], ref state[g + 2], ref state[g + 3]);

            uint first = state[0];
            Array.Copy(state, 1, state, 0, _words - 1);
            state[_words - 1] = first;

            if (((r + 1) & 3) == 0)
            {
                injection++;
                state[_words - 3] ^= _tweakSchedule[injection % 5];
                state[_words - 2] ^= _tweakSchedule[(injection + 1) % 5];
                state[_words - 1] ^= (uint)injection;
            }
        }

        AddRoundKey(state, _rounds - 1);
        for (int g = 0; g < _words; g += 4)
            SerpentReference.ApplySBox((_rounds - 1) & 7, ref state[g], ref state[g + 1], ref state[g + 2], ref state[g + 3]);

        AddRoundKey(state, _rounds);
        return WriteWords(state);
    }

    /// <summary>
    /// Decrypts one block.
    /// </summary>
    /// <param name="input">The ciphertext block.</param>
    /// <returns>The plaintext block.</returns>
    internal byte[] Decrypt(ReadOnlySpan<byte> input)
    {
        uint[] state = ReadWords(input);
        int injection = (_rounds / 4) - 1;

        AddRoundKey(state, _rounds);
        for (int g = 0; g < _words; g += 4)
            SerpentReference.ApplyInverseSBox((_rounds - 1) & 7, ref state[g], ref state[g + 1], ref state[g + 2], ref state[g + 3]);

        AddRoundKey(state, _rounds - 1);

        for (int r = _rounds - 2; r >= 0; r--)
        {
            if (((r + 1) & 3) == 0)
            {
                state[_words - 1] ^= (uint)injection;
                state[_words - 2] ^= _tweakSchedule[(injection + 1) % 5];
                state[_words - 3] ^= _tweakSchedule[injection % 5];
                injection--;
            }

            uint last = state[_words - 1];
            Array.Copy(state, 0, state, 1, _words - 1);
            state[0] = last;

            for (int g = 0; g < _words; g += 4)
                SerpentReference.InverseLinearTransform(ref state[g], ref state[g + 1], ref state[g + 2], ref state[g + 3]);

            for (int g = 0; g < _words; g += 4)
                SerpentReference.ApplyInverseSBox(r & 7, ref state[g], ref state[g + 1], ref state[g + 2], ref state[g + 3]);

            AddRoundKey(state, r);
        }

        return WriteWords(state);
    }

    /// <summary>
    /// Writes words little-endian into a new block.
    /// </summary>
    /// <param name="words">The words.</param>
    /// <returns>The block.</returns>
    private static byte[] WriteWords(uint[] words)
    {
        byte[] block = new byte[words.Length * 4];
        for (int i = 0; i < words.Length; i++)
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(i * 4), words[i]);

        return block;
    }

    /// <summary>
    /// Adds a whole round key to the state by XOR.
    /// </summary>
    /// <param name="state">The state.</param>
    /// <param name="round">The round whose key is added.</param>
    private void AddRoundKey(uint[] state, int round)
    {
        for (int i = 0; i < _words; i++)
            state[i] ^= _roundKeys[(round * _words) + i];
    }

    /// <summary>
    /// Reads a block as little-endian words.
    /// </summary>
    /// <param name="block">The block.</param>
    /// <returns>The words.</returns>
    private uint[] ReadWords(ReadOnlySpan<byte> block)
    {
        if (block.Length != _words * 4) throw new ArgumentException("The block must be as long as the key.", nameof(block));

        uint[] words = new uint[_words];
        for (int i = 0; i < _words; i++)
            words[i] = BinaryPrimitives.ReadUInt32LittleEndian(block[(i * 4)..]);

        return words;
    }
}
