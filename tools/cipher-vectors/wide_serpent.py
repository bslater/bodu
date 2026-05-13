"""Independent Python port of the wide-block tweakable Serpent variants
(Serpent-256, Serpent-512, Serpent-1024) used to cross-validate the C#
implementations in `Bodu.Security.Cryptography/src/Security.Cryptography/`.

The wide-block variants are non-standard constructions developed for this
library and have no externally published reference vectors. This script
provides a SECOND implementation, hand-translated from the C# sources, so
the KAT files in `Bodu.Security.Cryptography/test/Security.Cryptography/`
(Serpent256KnownAnswers.cs, Serpent512KnownAnswers.cs, Serpent1024KnownAnswers.cs)
can pin static ciphertexts that have been confirmed by an independent
implementation rather than by the cipher's own runtime output.

Source mapping:
  * Bodu.Security.Cryptography/src/Security.Cryptography/SerpentBlockCipherBase.cs
      → Phi, S-box tables, LinearTransform, ExpandPrekeys, KeyScheduleSBoxIndex
  * Bodu.Security.Cryptography/src/Security.Cryptography/SerpentBlockCipher.cs
      → wide-block round function, cross-lane rotation, tweak schedule
        [T0, T1, T2, T3, T0^T1^T2^T3], BuildRoundKeys
  * Bodu.Security.Cryptography/src/Security.Cryptography/SerpentBlockCipher.{256,512,1024}.cs
      → BlockWords / Rounds per variant

Usage:
    python3 tools/cipher-vectors/wide_serpent.py
Outputs the canonical ZeroedKeyAndTweak and DefaultKeyAndTweak vectors for
each wide-block variant, matching the rows pinned in the C# KAT files.
"""

import struct

PHI = 0x9E3779B9
MASK32 = 0xFFFFFFFF

SBOXES = [
    [3, 8, 15, 1, 10, 6, 5, 11, 14, 13, 4, 2, 7, 0, 9, 12],
    [15, 12, 2, 7, 9, 0, 5, 10, 1, 11, 14, 8, 6, 13, 3, 4],
    [8, 6, 7, 9, 3, 12, 10, 15, 13, 1, 14, 4, 0, 11, 5, 2],
    [0, 15, 11, 8, 12, 9, 6, 3, 13, 1, 2, 4, 10, 7, 5, 14],
    [1, 15, 8, 3, 12, 0, 11, 6, 2, 5, 4, 10, 9, 14, 7, 13],
    [15, 5, 2, 11, 4, 10, 9, 12, 0, 3, 14, 8, 13, 6, 7, 1],
    [7, 2, 12, 5, 8, 4, 6, 11, 14, 9, 1, 15, 13, 3, 10, 0],
    [1, 13, 15, 0, 14, 8, 2, 11, 7, 4, 12, 10, 9, 3, 5, 6],
]

INV_SBOXES = [
    [13, 3, 11, 0, 10, 6, 5, 12, 1, 14, 4, 7, 15, 9, 8, 2],
    [5, 8, 2, 14, 15, 6, 12, 3, 11, 4, 7, 9, 1, 13, 10, 0],
    [12, 9, 15, 4, 11, 14, 1, 2, 0, 3, 6, 13, 5, 8, 10, 7],
    [0, 9, 10, 7, 11, 14, 6, 13, 3, 5, 12, 2, 4, 8, 15, 1],
    [5, 0, 8, 3, 10, 9, 7, 14, 2, 12, 11, 6, 4, 15, 13, 1],
    [8, 15, 2, 9, 4, 1, 13, 14, 11, 6, 5, 3, 7, 12, 10, 0],
    [15, 10, 1, 13, 5, 3, 6, 0, 4, 9, 14, 7, 2, 12, 8, 11],
    [3, 0, 6, 13, 9, 14, 15, 8, 5, 12, 11, 7, 10, 1, 4, 2],
]


def rotl(x, n):
    n &= 31
    return ((x << n) | (x >> (32 - n))) & MASK32


def rotr(x, n):
    n &= 31
    return ((x >> n) | (x << (32 - n))) & MASK32


def apply_sbox(sbox_index, x):
    """Bitsliced S-box: 32 columns of 4-bit values across (x0, x1, x2, x3)."""
    table = SBOXES[sbox_index]
    y = [0, 0, 0, 0]
    x0, x1, x2, x3 = x
    for i in range(32):
        nibble = ((x0 >> i) & 1) | (((x1 >> i) & 1) << 1) | (((x2 >> i) & 1) << 2) | (((x3 >> i) & 1) << 3)
        s = table[nibble]
        y[0] |= (s & 1) << i
        y[1] |= ((s >> 1) & 1) << i
        y[2] |= ((s >> 2) & 1) << i
        y[3] |= ((s >> 3) & 1) << i
    return y


def apply_inv_sbox(sbox_index, x):
    table = INV_SBOXES[sbox_index]
    y = [0, 0, 0, 0]
    x0, x1, x2, x3 = x
    for i in range(32):
        nibble = ((x0 >> i) & 1) | (((x1 >> i) & 1) << 1) | (((x2 >> i) & 1) << 2) | (((x3 >> i) & 1) << 3)
        s = table[nibble]
        y[0] |= (s & 1) << i
        y[1] |= ((s >> 1) & 1) << i
        y[2] |= ((s >> 2) & 1) << i
        y[3] |= ((s >> 3) & 1) << i
    return y


def linear_transform(x):
    x0, x1, x2, x3 = x
    x0 = rotl(x0, 13)
    x2 = rotl(x2, 3)
    x1 = (x1 ^ x0 ^ x2) & MASK32
    x3 = (x3 ^ x2 ^ ((x0 << 3) & MASK32)) & MASK32
    x1 = rotl(x1, 1)
    x3 = rotl(x3, 7)
    x0 = (x0 ^ x1 ^ x3) & MASK32
    x2 = (x2 ^ x3 ^ ((x1 << 7) & MASK32)) & MASK32
    x0 = rotl(x0, 5)
    x2 = rotl(x2, 22)
    return [x0, x1, x2, x3]


def inverse_linear_transform(x):
    x0, x1, x2, x3 = x
    x2 = rotr(x2, 22)
    x0 = rotr(x0, 5)
    x2 = (x2 ^ x3 ^ ((x1 << 7) & MASK32)) & MASK32
    x0 = (x0 ^ x1 ^ x3) & MASK32
    x3 = rotr(x3, 7)
    x1 = rotr(x1, 1)
    x3 = (x3 ^ x2 ^ ((x0 << 3) & MASK32)) & MASK32
    x1 = (x1 ^ x0 ^ x2) & MASK32
    x2 = rotr(x2, 3)
    x0 = rotr(x0, 13)
    return [x0, x1, x2, x3]


def key_schedule_sbox_index(round_index):
    # canonical Serpent: K_0 → S3, K_1 → S2, …
    return (3 - round_index) & 7


def expand_prekeys(seed, total_len, window):
    """Mirrors SerpentBlockCipherBase.ExpandPrekeys."""
    prekeys = list(seed) + [0] * (total_len - window)
    for i in range(total_len - window):
        v = (prekeys[i] ^ prekeys[i + window - 5] ^ prekeys[i + window - 3]
             ^ prekeys[i + window - 1] ^ PHI ^ i)
        prekeys[i + window] = rotl(v & MASK32, 11)
    return prekeys


def build_tweak_schedule(tweak_bytes):
    assert len(tweak_bytes) == 16
    t0, t1, t2, t3 = struct.unpack('<4I', tweak_bytes)
    return [t0, t1, t2, t3, t0 ^ t1 ^ t2 ^ t3]


def build_round_keys(key_bytes, block_words, rounds):
    """Mirrors SerpentBlockCipher.BuildRoundKeys."""
    w = block_words
    seed = list(struct.unpack('<' + 'I' * w, key_bytes))
    prekey_len = w + (rounds + 1) * w
    prekeys = expand_prekeys(seed, prekey_len, w)
    round_keys = [0] * ((rounds + 1) * w)
    groups_per_rk = w // 4
    for r in range(rounds + 1):
        sbi = key_schedule_sbox_index(r)
        round_start = w + r * w
        for g in range(groups_per_rk):
            src = round_start + g * 4
            grp = apply_sbox(sbi, [prekeys[src], prekeys[src + 1], prekeys[src + 2], prekeys[src + 3]])
            dst = r * w + g * 4
            round_keys[dst:dst + 4] = grp
    return round_keys


def encrypt(plaintext, key, tweak, block_words, rounds):
    """Mirrors SerpentBlockCipher.Encrypt."""
    w = block_words
    assert len(plaintext) == w * 4
    assert len(key) == w * 4
    assert len(tweak) == 16

    state = list(struct.unpack('<' + 'I' * w, plaintext))
    rk = build_round_keys(key, w, rounds)
    tw = build_tweak_schedule(tweak)
    injection = 0

    for r in range(rounds - 1):
        for i in range(w):
            state[i] ^= rk[r * w + i]
        for g in range(0, w, 4):
            state[g:g + 4] = apply_sbox(r & 7, state[g:g + 4])
        for g in range(0, w, 4):
            state[g:g + 4] = linear_transform(state[g:g + 4])
        # Cross-lane permutation: rotate one position left.
        first = state[0]
        for i in range(w - 1):
            state[i] = state[i + 1]
        state[w - 1] = first

        # Tweak injection every fourth completed round.
        if ((r + 1) & 3) == 0:
            injection += 1
            state[w - 3] ^= tw[injection % 5]
            state[w - 2] ^= tw[(injection + 1) % 5]
            state[w - 1] ^= injection
            state[w - 3] &= MASK32
            state[w - 2] &= MASK32
            state[w - 1] &= MASK32

    # Final round: K, S-box, K.
    for i in range(w):
        state[i] ^= rk[(rounds - 1) * w + i]
    for g in range(0, w, 4):
        state[g:g + 4] = apply_sbox((rounds - 1) & 7, state[g:g + 4])
    for i in range(w):
        state[i] ^= rk[rounds * w + i]

    return struct.pack('<' + 'I' * w, *state)


def decrypt(ciphertext, key, tweak, block_words, rounds):
    """Mirrors SerpentBlockCipher.Decrypt."""
    w = block_words
    state = list(struct.unpack('<' + 'I' * w, ciphertext))
    rk = build_round_keys(key, w, rounds)
    tw = build_tweak_schedule(tweak)
    injection = (rounds // 4) - 1

    # Reverse the final Serpent-shaped round.
    for i in range(w):
        state[i] ^= rk[rounds * w + i]
    for g in range(0, w, 4):
        state[g:g + 4] = apply_inv_sbox((rounds - 1) & 7, state[g:g + 4])
    for i in range(w):
        state[i] ^= rk[(rounds - 1) * w + i]

    for r in range(rounds - 2, -1, -1):
        if ((r + 1) & 3) == 0:
            state[w - 1] ^= injection
            state[w - 2] ^= tw[(injection + 1) % 5]
            state[w - 3] ^= tw[injection % 5]
            state[w - 1] &= MASK32
            state[w - 2] &= MASK32
            state[w - 3] &= MASK32
            injection -= 1

        # Inverse cross-lane permutation: rotate one position right.
        last = state[w - 1]
        for i in range(w - 1, 0, -1):
            state[i] = state[i - 1]
        state[0] = last
        for g in range(0, w, 4):
            state[g:g + 4] = inverse_linear_transform(state[g:g + 4])
        for g in range(0, w, 4):
            state[g:g + 4] = apply_inv_sbox(r & 7, state[g:g + 4])
        for i in range(w):
            state[i] ^= rk[r * w + i]

    return struct.pack('<' + 'I' * w, *state)


# (block_words, rounds) per variant; matches SerpentBlockCipher.{256,512,1024}.cs.
CONFIGS = {
    'Serpent256': (8, 48),
    'Serpent512': (16, 64),
    'Serpent1024': (32, 80),
}


def _print_vector(label, key, tweak, plaintext, block_words, rounds):
    ct = encrypt(plaintext, key, tweak, block_words, rounds)
    rt = decrypt(ct, key, tweak, block_words, rounds)
    assert rt == plaintext, f'{label}: round-trip failed'
    print(f'  {label}:')
    print(f'    Key  = {key.hex().upper()}')
    print(f'    Tweak= {tweak.hex().upper()}')
    print(f'    PT   = {plaintext.hex().upper()}')
    print(f'    CT   = {ct.hex().upper()}')


def main():
    """Reproduces every row pinned in the C# KAT files."""
    for name, (bw, rd) in CONFIGS.items():
        n = bw * 4
        print(f'\n=== {name} (block={n}B, rounds={rd}) ===')

        # ZeroedKeyAndTweak: all zeros.
        _print_vector('ZeroedKeyAndTweak', bytes(n), bytes(16), bytes(n), bw, rd)

        # DefaultKeyAndTweak: incremental key/tweak, descending plaintext.
        key = bytes((0x10 + i) & 0xFF for i in range(n))
        tweak = bytes(range(16))
        plaintext = bytes((0xFF - i) & 0xFF for i in range(n))
        _print_vector('DefaultKeyAndTweak', key, tweak, plaintext, bw, rd)


if __name__ == '__main__':
    main()
